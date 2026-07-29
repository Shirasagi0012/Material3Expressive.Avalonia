#!/usr/bin/env python3
"""Same-coordinate diagnostic analysis for visual-parity expected/actual PNGs.

This script explains image differences. It does not implement or override the repository's
official semantic raster policy.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

try:
    import cv2
    import numpy as np
    from PIL import Image
except ImportError as error:
    package = {"PIL": "pillow", "numpy": "numpy", "cv2": "opencv-python"}.get(error.name, error.name)
    raise SystemExit(
        f"Missing Python dependency '{error.name}'. Ask the user before installing package '{package}'."
    ) from error


DEFAULT_BINS = "1,2,8,10,18,20,35,48,255"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Analyze same-coordinate expected/actual parity PNG differences."
    )
    parser.add_argument(
        "source",
        help="Case artifact directory containing expected.png/image.png, or expected PNG path.",
    )
    parser.add_argument("actual", nargs="?", help="Actual PNG path when source is an image.")
    parser.add_argument(
        "--output-dir",
        help="Optional directory for report.json, heatmap, mask, strip, crop, and blink GIF.",
    )
    parser.add_argument(
        "--backdrop",
        default="#FFFFFFFF",
        help="RGBA comparison backdrop for visual RGB, as #RRGGBB or #RRGGBBAA (default: white).",
    )
    parser.add_argument(
        "--threshold",
        type=int,
        default=0,
        help="Diagnostic stored-RGBA delta floor for masks/statistics; does not affect official verdict.",
    )
    parser.add_argument(
        "--bins",
        default=DEFAULT_BINS,
        help=f"Comma-separated inclusive stored-RGBA delta bin maxima (default: {DEFAULT_BINS}).",
    )
    parser.add_argument("--top", type=int, default=10, help="Maximum components/transitions/rows to print.")
    parser.add_argument("--scale", type=int, default=8, help="Nearest-neighbor crop scale.")
    parser.add_argument("--padding", type=int, default=3, help="Pixels around the difference bbox in crop.")
    return parser.parse_args()


def resolve_paths(source_value: str, actual_value: str | None) -> tuple[Path, Path, Path | None]:
    source = Path(source_value).resolve()
    if not source.exists():
        raise FileNotFoundError(source)
    if source.is_dir():
        if actual_value is not None:
            raise ValueError("Do not provide an actual image when source is a case directory.")
        expected = source / "expected.png"
        actual = source / "image.png"
        comparison = source / "comparison.json"
        return expected, actual, comparison if comparison.is_file() else None

    if actual_value is None:
        raise ValueError("Provide the actual PNG path when source is an expected image.")
    return source, Path(actual_value).resolve(), None


def parse_backdrop(value: str) -> tuple[int, int, int]:
    text = value.strip().lstrip("#")
    if len(text) not in (6, 8):
        raise ValueError("Backdrop must be #RRGGBB or #RRGGBBAA.")
    channels = tuple(int(text[index : index + 2], 16) for index in range(0, len(text), 2))
    if len(channels) == 3:
        return channels
    alpha = channels[3]
    return tuple((channel * alpha + 255 * (255 - alpha) + 127) // 255 for channel in channels[:3])


def parse_bins(value: str) -> list[int]:
    bins = sorted({int(item.strip()) for item in value.split(",") if item.strip()})
    if not bins or bins[0] < 1 or bins[-1] > 255:
        raise ValueError("Bins must contain ascending values between 1 and 255.")
    if bins[-1] != 255:
        bins.append(255)
    return bins


def load_rgba(path: Path) -> np.ndarray:
    if not path.is_file():
        raise FileNotFoundError(path)
    with Image.open(path) as image:
        return np.array(image.convert("RGBA"), dtype=np.uint8, copy=True)


def composite_visual_rgb(rgba: np.ndarray, backdrop: tuple[int, int, int]) -> np.ndarray:
    values = rgba.astype(np.uint16)
    alpha = values[..., 3:4]
    background = np.array(backdrop, dtype=np.uint16).reshape(1, 1, 3)
    return ((values[..., :3] * alpha + background * (255 - alpha) + 127) // 255).astype(np.uint8)


def maximum_delta(left: np.ndarray, right: np.ndarray) -> np.ndarray:
    return np.max(np.abs(left.astype(np.int16) - right.astype(np.int16)), axis=2).astype(np.uint8)


def bounding_box(mask: np.ndarray) -> list[int] | None:
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return None
    return [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]


def delta_histogram(delta: np.ndarray, mask: np.ndarray, bins: list[int]) -> list[dict[str, int]]:
    values = delta[mask]
    result: list[dict[str, int]] = []
    lower = 1
    for upper in bins:
        selected = values[(values >= lower) & (values <= upper)]
        result.append(
            {
                "minimumDelta": lower,
                "maximumDelta": upper,
                "pixels": int(selected.size),
                "energy": int(selected.astype(np.uint64).sum()),
            }
        )
        lower = upper + 1
    return result


def connected_components(mask: np.ndarray, delta: np.ndarray, top: int) -> list[dict[str, int]]:
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), connectivity=8)
    result: list[dict[str, int]] = []
    for label in range(1, count):
        left, y, width, height, area = (int(value) for value in stats[label])
        component_values = delta[labels == label]
        result.append(
            {
                "left": left,
                "top": y,
                "right": left + width,
                "bottom": y + height,
                "pixels": area,
                "thicknessPx": min(width, height),
                "maximumDelta": int(component_values.max(initial=0)),
                "energy": int(component_values.astype(np.uint64).sum()),
            }
        )
    result.sort(key=lambda item: (item["pixels"], item["maximumDelta"], item["energy"]), reverse=True)
    return result[:top]


def top_axis_counts(mask: np.ndarray, axis: int, top: int) -> list[dict[str, int]]:
    counts = mask.sum(axis=axis)
    indices = np.flatnonzero(counts)
    ordered = sorted(indices, key=lambda index: (-int(counts[index]), int(index)))[:top]
    label = "x" if axis == 0 else "y"
    return [{label: int(index), "pixels": int(counts[index])} for index in ordered]


def top_transitions(
    expected: np.ndarray,
    actual: np.ndarray,
    mask: np.ndarray,
    top: int,
    expected_key: str,
    actual_key: str,
) -> list[dict[str, Any]]:
    if not np.any(mask):
        return []
    pairs = np.concatenate((expected[mask], actual[mask]), axis=1)
    unique, counts = np.unique(pairs, axis=0, return_counts=True)
    order = np.argsort(counts)[::-1][:top]
    channels = expected.shape[2]
    return [
        {
            expected_key: [int(value) for value in unique[index, :channels]],
            actual_key: [int(value) for value in unique[index, channels:]],
            "pixels": int(counts[index]),
        }
        for index in order
    ]


def infer_failure_layer(failures: list[str]) -> str:
    if not failures:
        return "none"
    lowered = [failure.lower() for failure in failures]
    if any("text " in item or "text layout" in item for item in lowered):
        return "TextLayout"
    if any("shadow" in item for item in lowered):
        return "ShadowContract"
    if any("node " in item or "bounds" in item for item in lowered):
        return "Geometry"
    if any("viewport" in item or "coordinate-space" in item or "contract" in item for item in lowered):
        return "Contract"
    if all(item.startswith("raster differs") for item in lowered):
        return "RasterMismatch"
    return "SemanticOrRasterMismatch"


def read_official_report(path: Path | None, storage_exact: bool) -> dict[str, Any] | None:
    if path is None:
        return None
    with path.open("r", encoding="utf-8-sig") as stream:
        comparison = json.load(stream)
    is_match = bool(comparison.get("isMatch"))
    failures = comparison.get("failures") or []
    warnings = comparison.get("warnings") or []
    kind = comparison.get("kind")
    kind_source = "comparison.json"
    if kind is None:
        kind_source = "legacy-derived"
        kind = (
            "rasterWarning"
            if is_match and warnings
            else "exactMatch"
            if is_match and storage_exact
            else "semanticRasterEquivalent"
            if is_match
            else "mismatch"
        )
    return {
        "path": str(path),
        "profileId": comparison.get("profileId"),
        "isMatch": is_match,
        "kind": kind,
        "kindSource": kind_source,
        "failureLayer": "RasterWarning" if kind == "rasterWarning" else infer_failure_layer(failures),
        "failures": failures,
        "warnings": warnings,
        "pixels": comparison.get("pixels"),
        "raster": comparison.get("raster"),
        "warningRaster": comparison.get("warningRaster"),
    }


def save_rgb(path: Path, rgb: np.ndarray) -> None:
    Image.fromarray(rgb, mode="RGB").save(path)


def make_heatmap(delta: np.ndarray, mask: np.ndarray) -> np.ndarray:
    result = np.zeros((*delta.shape, 3), dtype=np.uint8)
    values = delta.astype(np.int32)
    result[..., 0] = np.where(mask, 255, 0).astype(np.uint8)
    result[..., 1] = np.where(mask, np.maximum(0, 255 - values * 8), 0).astype(np.uint8)
    return result


def make_strip(images: list[np.ndarray], separator: int = 2) -> np.ndarray:
    height = max(image.shape[0] for image in images)
    width = sum(image.shape[1] for image in images) + separator * (len(images) - 1)
    result = np.full((height, width, 3), 32, dtype=np.uint8)
    x = 0
    for image in images:
        result[: image.shape[0], x : x + image.shape[1]] = image
        x += image.shape[1] + separator
    return result


def write_outputs(
    output_dir: Path,
    report: dict[str, Any],
    expected: np.ndarray,
    actual: np.ndarray,
    delta: np.ndarray,
    mask: np.ndarray,
    bbox: list[int] | None,
    scale: int,
    padding: int,
) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    with (output_dir / "report.json").open("w", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2, ensure_ascii=False)
        stream.write("\n")

    heatmap = make_heatmap(delta, mask)
    save_rgb(output_dir / "visual-expected.png", expected)
    save_rgb(output_dir / "visual-actual.png", actual)
    save_rgb(output_dir / "delta-heatmap.png", heatmap)
    binary = np.repeat((mask.astype(np.uint8) * 255)[..., None], 3, axis=2)
    save_rgb(output_dir / "difference-mask.png", binary)
    save_rgb(output_dir / "comparison-strip.png", make_strip([expected, actual, heatmap]))

    frames = [Image.fromarray(expected, mode="RGB"), Image.fromarray(actual, mode="RGB")]
    frames[0].save(
        output_dir / "blink.gif",
        save_all=True,
        append_images=frames[1:],
        duration=500,
        loop=0,
        disposal=2,
    )

    if bbox is not None:
        left, top, right, bottom = bbox
        left = max(0, left - padding)
        top = max(0, top - padding)
        right = min(expected.shape[1], right + padding)
        bottom = min(expected.shape[0], bottom + padding)
        crop = make_strip(
            [
                expected[top:bottom, left:right],
                actual[top:bottom, left:right],
                heatmap[top:bottom, left:right],
            ]
        )
        image = Image.fromarray(crop, mode="RGB")
        image = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
        image.save(output_dir / f"difference-crop-x{scale}.png")


def print_report(report: dict[str, Any], top: int) -> None:
    image = report["image"]
    stored = report["storedRgba"]
    visual = report["visualRgb"]
    print(f"Expected: {image['expectedPath']}")
    print(f"Actual:   {image['actualPath']}")
    print(f"Size: {image['width']}x{image['height']}")
    print(f"Stored RGBA exact: {stored['exact']}")
    print(
        "Stored RGBA differences: "
        f"{stored['differentPixels']}/{image['totalPixels']} pixels, "
        f"max delta {stored['maximumDelta']}, energy {stored['energy']}, bbox {stored['bbox']}"
    )
    print(f"Alpha-storage differences: {stored['alphaDifferentPixels']}")
    print(f"Visual RGB exact: {visual['exact']} over {visual['backdrop']}")
    print(
        "Visual RGB differences: "
        f"{visual['differentPixels']}/{image['totalPixels']} pixels, "
        f"max delta {visual['maximumDelta']}, energy {visual['energy']}, bbox {visual['bbox']}"
    )

    print("Stored RGBA delta bins:")
    for item in report["deltaBins"]:
        print(
            f"  {item['minimumDelta']:>3}-{item['maximumDelta']:<3}: "
            f"pixels={item['pixels']}, energy={item['energy']}"
        )

    print("Largest components:")
    for item in report["largestComponents"][:top]:
        print(
            f"  bbox=({item['left']},{item['top']},{item['right']},{item['bottom']}), "
            f"pixels={item['pixels']}, thickness={item['thicknessPx']}, "
            f"max={item['maximumDelta']}, energy={item['energy']}"
        )

    official = report.get("official")
    if official is not None:
        print(
            f"Official: isMatch={official['isMatch']}, profile={official['profileId']}, "
            f"kind={official['kind']} ({official['kindSource']})"
        )
        if official["kind"] == "mismatch":
            print(f"  first inferred failure layer: {official['failureLayer']}")
        elif official["kind"] == "rasterWarning":
            print("  normal raster policy rejected; secondary warning policy accepted")
        for failure in official["failures"]:
            print(f"  failure: {failure}")
        for warning in official["warnings"]:
            print(f"  warning: {warning}")
        raster = official.get("raster") or {}
        print(
            "  normal raster: "
            f"floor={raster.get('ignoredFloorPixels')}, allowed={raster.get('allowedPixels')}, "
            f"rejected={raster.get('rejectedPixels')}, maxRejected={raster.get('maximumRejectedChannelDelta')}"
        )
        warning_raster = official.get("warningRaster")
        if warning_raster is not None:
            print(
                "  warning raster: "
                f"floor={warning_raster.get('ignoredFloorPixels')}, "
                f"allowed={warning_raster.get('allowedPixels')}, "
                f"rejected={warning_raster.get('rejectedPixels')}, "
                f"maxRejected={warning_raster.get('maximumRejectedChannelDelta')}"
            )
    else:
        print("Official: no comparison.json supplied; diagnostics do not determine pass/fail.")


def main() -> int:
    args = parse_args()
    if not 0 <= args.threshold <= 255:
        raise ValueError("Threshold must be between 0 and 255.")
    if args.top < 1 or args.scale < 1 or args.padding < 0:
        raise ValueError("Top and scale must be positive; padding must be non-negative.")

    expected_path, actual_path, comparison_path = resolve_paths(args.source, args.actual)
    backdrop = parse_backdrop(args.backdrop)
    bins = parse_bins(args.bins)
    expected_rgba = load_rgba(expected_path)
    actual_rgba = load_rgba(actual_path)
    if expected_rgba.shape != actual_rgba.shape:
        raise ValueError(
            f"Image dimensions differ: expected {expected_rgba.shape[1]}x{expected_rgba.shape[0]}, "
            f"actual {actual_rgba.shape[1]}x{actual_rgba.shape[0]}."
        )

    expected_visual = composite_visual_rgb(expected_rgba, backdrop)
    actual_visual = composite_visual_rgb(actual_rgba, backdrop)
    stored_delta = maximum_delta(expected_rgba, actual_rgba)
    visual_delta = maximum_delta(expected_visual, actual_visual)
    stored_mask = stored_delta > args.threshold
    visual_mask = visual_delta > args.threshold
    storage_exact = bool(np.array_equal(expected_rgba, actual_rgba))
    visual_exact = bool(np.array_equal(expected_visual, actual_visual))
    stored_bbox = bounding_box(stored_mask)
    visual_bbox = bounding_box(visual_mask)
    total_pixels = int(stored_delta.size)
    stored_different_pixels = int(stored_mask.sum())
    visual_different_pixels = int(visual_mask.sum())

    report: dict[str, Any] = {
        "schemaVersion": 3,
        "diagnosticOnly": True,
        "image": {
            "expectedPath": str(expected_path),
            "actualPath": str(actual_path),
            "width": int(stored_delta.shape[1]),
            "height": int(stored_delta.shape[0]),
            "totalPixels": total_pixels,
        },
        "storedRgba": {
            "exact": storage_exact,
            "diagnosticThreshold": args.threshold,
            "differentPixels": stored_different_pixels,
            "differentRatio": stored_different_pixels / total_pixels,
            "maximumDelta": int(stored_delta.max(initial=0)),
            "energy": int(stored_delta[stored_mask].astype(np.uint64).sum()),
            "bbox": stored_bbox,
            "alphaDifferentPixels": int(np.count_nonzero(expected_rgba[..., 3] != actual_rgba[..., 3])),
        },
        "visualRgb": {
            "exact": visual_exact,
            "backdrop": f"#{backdrop[0]:02X}{backdrop[1]:02X}{backdrop[2]:02X}",
            "diagnosticThreshold": args.threshold,
            "differentPixels": visual_different_pixels,
            "differentRatio": visual_different_pixels / total_pixels,
            "maximumDelta": int(visual_delta.max(initial=0)),
            "energy": int(visual_delta[visual_mask].astype(np.uint64).sum()),
            "bbox": visual_bbox,
        },
        "deltaBins": delta_histogram(stored_delta, stored_mask, bins),
        "largestComponents": connected_components(stored_mask, stored_delta, args.top),
        "topRows": top_axis_counts(stored_mask, axis=1, top=args.top),
        "topColumns": top_axis_counts(stored_mask, axis=0, top=args.top),
        "topStoredRgbaTransitions": top_transitions(
            expected_rgba,
            actual_rgba,
            stored_mask,
            args.top,
            "expectedRgba",
            "actualRgba",
        ),
        "topVisualRgbTransitions": top_transitions(
            expected_visual,
            actual_visual,
            visual_mask,
            args.top,
            "expectedRgb",
            "actualRgb",
        ),
        "official": read_official_report(comparison_path, storage_exact),
    }

    print_report(report, args.top)
    if args.output_dir:
        output_dir = Path(args.output_dir).resolve()
        write_outputs(
            output_dir,
            report,
            expected_visual,
            actual_visual,
            stored_delta,
            stored_mask,
            stored_bbox,
            args.scale,
            args.padding,
        )
        print(f"Diagnostics written to: {output_dir}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (FileNotFoundError, ValueError, json.JSONDecodeError) as error:
        print(f"error: {error}", file=sys.stderr)
        raise SystemExit(2) from error
