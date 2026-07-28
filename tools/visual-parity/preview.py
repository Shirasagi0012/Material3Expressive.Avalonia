#!/usr/bin/env python3
"""Visual parity report previewer.

Generates an HTML report from a parity run directory and serves it via
a lightweight HTTP server (avoids file:// CORS restrictions).
Usage: python tools/visual-parity/preview.py [run-directory]
"""

import json
import os
import sys
import webbrowser
from pathlib import Path
from string import Template
from http.server import HTTPServer, SimpleHTTPRequestHandler
import threading
import socket

PORT = 8765

HTML_TEMPLATE = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Visual Parity Report — ${run_id}</title>
<style>
:root { color-scheme: dark; }
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font: 13px/1.5 system-ui, sans-serif; background: #0d1117; color: #c9d1d9; display: flex; height: 100vh; overflow: hidden; }

#sidebar { width: 320px; min-width: 260px; background: #161b22; border-right: 1px solid #30363d; display: flex; flex-direction: column; }
#stats { padding: 12px 16px; border-bottom: 1px solid #30363d; display: flex; gap: 8px; flex-wrap: wrap; }
.stat { padding: 4px 10px; border-radius: 12px; font-size: 12px; font-weight: 600; }
.stat-pass { background: #1a3a2a; color: #3fb950; }
.stat-fail { background: #3a1a1a; color: #f85149; }
.stat-total { background: #1a2a3a; color: #58a6ff; }
#filter { margin: 8px 12px; padding: 6px 10px; border: 1px solid #30363d; border-radius: 6px; background: #0d1117; color: #c9d1d9; font-size: 12px; outline: none; width: calc(100% - 24px); }
#filter:focus { border-color: #58a6ff; }
#case-list { flex: 1; overflow-y: auto; }
.case-item { padding: 7px 14px; cursor: pointer; border-bottom: 1px solid #21262d; font-size: 12px; display: flex; align-items: center; gap: 6px; }
.case-item:hover { background: #1c2129; }
.case-item.active { background: #1f2a37; border-left: 3px solid #58a6ff; padding-left: 11px; }
.case-item .icon { font-size: 14px; flex-shrink: 0; }
.case-id { overflow: hidden; text-overflow: ellipsis; }
.case-id .family { color: #8b949e; font-size: 11px; }

#main { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
#tabs { display: flex; border-bottom: 1px solid #30363d; padding: 0 12px; gap: 2px; background: #161b22; }
.tab { padding: 8px 14px; cursor: pointer; border: none; background: none; color: #8b949e; font-size: 12px; border-bottom: 2px solid transparent; }
.tab:hover { color: #c9d1d9; }
.tab.active { color: #f0f6fc; border-bottom-color: #f78166; }
#content { flex: 1; overflow-y: auto; padding: 16px; }

#images-panel { display: flex; flex-wrap: wrap; gap: 16px; }
.img-card { flex: 1; min-width: 280px; }
.img-card h3 { font-size: 11px; font-weight: 600; color: #8b949e; margin-bottom: 6px; text-transform: uppercase; letter-spacing: .5px; }
.img-card img { max-width: 100%; border: 1px solid #30363d; border-radius: 4px; image-rendering: auto; }
.img-card .dims { font-size: 10px; color: #484f58; margin-top: 4px; }

table { width: 100%; border-collapse: collapse; font-size: 12px; }
th, td { padding: 5px 10px; text-align: right; border-bottom: 1px solid #21262d; }
th { text-align: right; color: #8b949e; font-weight: 600; white-space: nowrap; }
th:first-child, td:first-child { text-align: left; }
td.mono { font-family: 'SF Mono','Cascadia Code',monospace; font-size: 11px; }
.match { color: #3fb950; }
.mismatch { color: #f85149; }
.failure { color: #f85149; font-size: 12px; padding: 3px 0; }
.failure:before { content: "\2717 "; }
.empty { color: #484f58; padding: 40px; text-align: center; font-size: 13px; }
.sig { font-family: 'SF Mono','Cascadia Code',monospace; font-size: 10px; color: #484f58; white-space: pre-wrap; word-break: break-all; }
#detail-header { margin-bottom: 12px; }
#detail-header h2 { font-size: 15px; font-weight: 600; margin-bottom: 4px; word-break: break-all; }
#detail-header .meta { font-size: 11px; color: #484f58; }
#legend { padding: 6px 12px; border-top: 1px solid #30363d; font-size: 10px; color: #484f58; display: flex; gap: 12px; flex-wrap: wrap; }

@media (max-width: 700px) {
  body { flex-direction: column; }
  #sidebar { width: 100%; min-width: 0; max-height: 40vh; }
}
</style>
</head>
<body>
<div id="sidebar">
  <div id="stats"></div>
  <input id="filter" placeholder="Filter cases..." autofocus>
  <div id="case-list"></div>
  <div id="legend">
    <span>&check; pass</span><span>&#10005; fail (geometry)</span><span>&#11041; fail (raster)</span><span>&#9679; fail (text)</span>
  </div>
</div>
<div id="main">
  <div id="tabs">
    <button class="tab active" data-panel="images">Images</button>
    <button class="tab" data-panel="geometry">Geometry</button>
    <button class="tab" data-panel="textlayout">Text Layout</button>
    <button class="tab" data-panel="failures">Failures</button>
    <button class="tab" data-panel="raw">Raw JSON</button>
  </div>
  <div id="content">
    <div id="detail-header"><h2>&nbsp;</h2><div class="meta"></div></div>
    <div id="images-panel"></div>
    <div id="geometry-panel" style="display:none"></div>
    <div id="textlayout-panel" style="display:none"></div>
    <div id="failures-panel" style="display:none"></div>
    <div id="raw-panel" style="display:none"></div>
  </div>
</div>

<script>
var EXPECTED_MANIFEST = ${expected_json};
var ACTUAL_MANIFEST = ${actual_json};
var COMPARISONS = ${comparisons_json};
var EXPECTED_LAYOUTS = ${expected_layouts_json};
var ACTUAL_LAYOUTS = ${actual_layouts_json};
var IMAGE_PATHS = ${image_paths_json};
var CASE_FAMILIES = ${case_families_json};

var cases = ACTUAL_MANIFEST.cases || [];
var caseMap = {};
cases.forEach(function(c) { caseMap[c.id] = c; });
var activeCaseId = null;

(function renderStats() {
    var pass = 0, fail = 0;
    cases.forEach(function(c) { c.isMatch ? pass++ : fail++; });
    document.getElementById('stats').innerHTML =
        '<span class="stat stat-pass">PASS ' + pass + '</span>' +
        '<span class="stat stat-fail">FAIL ' + fail + '</span>' +
        '<span class="stat stat-total">TOTAL ' + cases.length + '</span>';
})();

var listEl = document.getElementById('case-list');
var filterEl = document.getElementById('filter');

function getFailureIcon(caseId) {
    var c = caseMap[caseId];
    if (!c || c.isMatch) return '<span class="icon match">\u2713</span>';
    var comp = COMPARISONS[caseId];
    if (!comp) return '<span class="icon mismatch">\u2717</span>';
    var failures = comp.failures || [];
    var hasGeom = failures.some(function(f) { return f.indexOf('bounds') >= 0; });
    var hasText = failures.some(function(f) { return f.indexOf('Text') >= 0 || f.indexOf('baseline') >= 0 || f.indexOf('UTF') >= 0; });
    if (hasGeom && hasText) return '<span class="icon mismatch" style="color:#d2991d">\u25C9</span>';
    if (hasGeom) return '<span class="icon mismatch" style="color:#f85149">\u2715</span>';
    if (hasText) return '<span class="icon mismatch" style="color:#d2991d">\u25CF</span>';
    return '<span class="icon mismatch" style="color:#f0883e">\u2B21</span>';
}

function renderCaseList(filter) {
    var ids = cases.map(function(c) { return c.id; });
    if (filter) {
        var lower = filter.toLowerCase();
        ids = ids.filter(function(id) { return id.toLowerCase().indexOf(lower) >= 0; });
    }
    var html = '';
    ids.forEach(function(id) {
        var family = CASE_FAMILIES[id] || id.split('/')[0];
        html += '<div class="case-item" data-id="' + id.replace(/"/g, '&quot;') + '">' +
            getFailureIcon(id) +
            '<span class="case-id" title="' + id.replace(/"/g, '&quot;') + '">' +
            '<span class="family">' + family.replace(/</g, '&lt;') + '/</span>' +
            id.substring(family.length + 1).replace(/</g, '&lt;') +
            '</span></div>';
    });
    listEl.innerHTML = html || '<div class="empty">No cases match filter</div>';
}

renderCaseList('');
if (cases.length > 0) selectCase(cases[0].id);

filterEl.addEventListener('input', function() { renderCaseList(this.value); selectFirstVisible(); });

listEl.addEventListener('click', function(e) {
    var item = e.target.closest('.case-item');
    if (item) selectCase(item.dataset.id);
});

function selectFirstVisible() {
    var first = listEl.querySelector('.case-item');
    if (first) selectCase(first.dataset.id);
}

document.getElementById('tabs').addEventListener('click', function(e) {
    if (!e.target.classList.contains('tab')) return;
    document.querySelectorAll('.tab').forEach(function(t) { t.classList.remove('active'); });
    e.target.classList.add('active');
    var panel = e.target.dataset.panel;
    document.getElementById('images-panel').style.display = panel === 'images' ? 'flex' : 'none';
    ['geometry-panel','textlayout-panel','failures-panel','raw-panel'].forEach(function(id) {
        document.getElementById(id).style.display = (id === panel + '-panel') ? '' : 'none';
    });
});

function selectCase(caseId) {
    activeCaseId = caseId;
    document.querySelectorAll('.case-item').forEach(function(el) {
        el.classList.toggle('active', el.dataset.id === caseId);
    });
    renderDetail(caseId);
    renderImages(caseId);
    renderGeometry(caseId);
    renderTextLayout(caseId);
    renderFailures(caseId);
    renderRaw(caseId);
}

function renderDetail(caseId) {
    var c = caseMap[caseId];
    var comp = COMPARISONS[caseId];
    var pixels = comp ? comp.pixels : null;
    var meta = '';
    if (c) meta += 'Match: ' + (c.isMatch ? 'YES' : 'NO') + ' \u2022 ';
    if (pixels) meta += 'Diff: ' + pixels.differentPixels + '/' + pixels.totalPixels + ' px (max delta ' + pixels.maximumChannelDelta + ')';
    document.getElementById('detail-header').innerHTML =
        '<h2>' + caseId.replace(/</g, '&lt;') + '</h2><div class="meta">' + meta + '</div>';
}

function renderImages(caseId) {
    var imgs = IMAGE_PATHS[caseId];
    if (!imgs) { document.getElementById('images-panel').innerHTML = '<div class="empty">No images</div>'; return; }
    var comp = COMPARISONS[caseId];
    var px = comp ? comp.pixels : null;
    var scale = EXPECTED_MANIFEST.viewportScale || 2.0;
    var getLogical = function(p) { return p ? Math.round(p / scale) : '?'; };
    document.getElementById('images-panel').innerHTML =
        '<div class="img-card"><h3>Expected (Compose Desktop)</h3>' +
        '<img src="' + imgs.expected.replace(/"/g, '&quot;') + '" alt="expected">' +
        '<div class="dims">' + getLogical(imgs.expectedW) + '\u00d7' + getLogical(imgs.expectedH) + ' dp</div></div>' +
        '<div class="img-card"><h3>Actual (Avalonia)</h3>' +
        '<img src="' + imgs.actual.replace(/"/g, '&quot;') + '" alt="actual">' +
        '<div class="dims">' + getLogical(imgs.actualW) + '\u00d7' + getLogical(imgs.actualH) + ' dp</div></div>' +
        '<div class="img-card"><h3>Diff' + (px ? ' (' + px.differentPixels + ' px, max\u0394=' + px.maximumChannelDelta + ')' : '') + '</h3>' +
        '<img src="' + imgs.diff.replace(/"/g, '&quot;') + '" alt="diff" style="background:#0d1117">' +
        '</div>';
}

function renderGeometry(caseId) {
    var exp = EXPECTED_LAYOUTS[caseId];
    var act = ACTUAL_LAYOUTS[caseId];
    if (!exp) { document.getElementById('geometry-panel').innerHTML = '<div class="empty">No expected layout data</div>'; return; }

    var expMap = {};
    (exp.nodes || []).forEach(function(n) { expMap[n.id] = n; });
    var actMap = {};
    if (act) (act.nodes || []).forEach(function(n) { actMap[n.id] = n; });

    var rows = '';
    (exp.nodes || []).forEach(function(n) {
        var a = actMap[n.id];
        var eb = n.boundsInRootPx || {};
        var ab = a ? (a.boundsInRootPx || {}) : {};
        var f = function(v) { return v != null ? v.toFixed(1) : '\u2014'; };
        var dl = a && eb.left != null && ab.left != null ? (ab.left - eb.left).toFixed(1) : '\u2014';
        var dt = a && eb.top != null && ab.top != null ? (ab.top - eb.top).toFixed(1) : '\u2014';
        var dr = a && eb.right != null && ab.right != null ? (ab.right - eb.right).toFixed(1) : '\u2014';
        var db = a && eb.bottom != null && ab.bottom != null ? (ab.bottom - eb.bottom).toFixed(1) : '\u2014';
        var cls = a ? (dl === '0.0' && dt === '0.0' && dr === '0.0' && db === '0.0' ? 'match' : 'mismatch') : 'mismatch';
        rows += '<tr class="' + cls + '"><td>' + n.id.replace(/</g, '&lt;') + ' <span style="color:#484f58">(' + (n.kind||'').replace(/</g, '&lt;') + ')</span></td>' +
            '<td class="mono">' + f(eb.left) + ',' + f(eb.top) + ' \u2192 ' + f(eb.right) + ',' + f(eb.bottom) + '</td>' +
            '<td class="mono">' + f(ab.left) + ',' + f(ab.top) + ' \u2192 ' + f(ab.right) + ',' + f(ab.bottom) + '</td>' +
            '<td class="mono ' + cls + '">' + dl + ' / ' + dt + ' / ' + dr + ' / ' + db + '</td></tr>';
    });

    document.getElementById('geometry-panel').innerHTML =
        '<table><thead><tr><th>Node</th><th>Expected (px)</th><th>Actual (px)</th><th>\u0394 L/T/R/B</th></tr></thead><tbody>' +
        rows + '</tbody></table>';
}

function renderTextLayout(caseId) {
    var exp = EXPECTED_LAYOUTS[caseId];
    var act = ACTUAL_LAYOUTS[caseId];
    var textNodes = (exp && exp.nodes || []).filter(function(n) { return n.textLayout; });
    if (textNodes.length === 0) {
        document.getElementById('textlayout-panel').innerHTML = '<div class="empty">No text layout data for this case</div>';
        return;
    }

    var html = '';
    textNodes.forEach(function(en) {
        var an = act ? (act.nodes || []).find(function(n) { return n.id === en.id; }) : null;
        var et = en.textLayout;
        var at = an ? an.textLayout : null;
        html += '<h3 style="margin:12px 0 6px">Node: <code>' + en.id.replace(/</g, '&lt;') + '</code> &mdash; &ldquo;' + (et.text||'').replace(/</g, '&lt;') + '&rdquo;</h3>';

        html += '<table><thead><tr><th>Metric</th><th>Expected</th><th>Actual</th><th>\u0394</th></tr></thead><tbody>';
        var q = function(label, expVal, actVal) {
            var d = (expVal != null && actVal != null) ? (actVal - expVal).toFixed(3) : '\u2014';
            var cls2 = d === '0.000' ? 'match' : 'mismatch';
            html += '<tr class="' + cls2 + '"><td>' + label + '</td><td class="mono">' + (expVal != null ? expVal.toFixed(3) : '-') +
                '</td><td class="mono">' + (actVal != null ? actVal.toFixed(3) : '-') + '</td><td class="mono ' + cls2 + '">' + d + '</td></tr>';
        };
        q('Line count', et.lineCount, at ? at.lineCount : null);
        q('First baseline (px)', et.firstBaselineInRootPx, at ? at.firstBaselineInRootPx : null);
        q('Last baseline (px)', et.lastBaselineInRootPx, at ? at.lastBaselineInRootPx : null);
        html += '</tbody></table>';

        if (et.lines && et.lines.length > 0) {
            html += '<h4 style="margin:10px 0 4px;font-size:11px;color:#8b949e">Lines</h4>';
            html += '<table><thead><tr><th>#</th><th>UTF-16</th><th>Baseline (px)</th><th>Expected \u0394</th></tr></thead><tbody>';
            (et.lines || []).forEach(function(el, i) {
                var al = (at && at.lines) ? at.lines[i] : null;
                var d = al ? (al.baselinePx - el.baselinePx).toFixed(3) : '\u2014';
                html += '<tr class="' + (d === '0.000' ? 'match' : 'mismatch') + '"><td>' + i + '</td><td class="mono">' +
                    el.startUtf16 + '\u2013' + el.endUtf16 + '</td><td class="mono">' +
                    el.baselinePx.toFixed(3) + (al ? ' / ' + al.baselinePx.toFixed(3) : '') +
                    '</td><td class="mono">' + d + '</td></tr>';
            });
            html += '</tbody></table>';
        }

        if (et.characterBoxes && et.characterBoxes.length > 0) {
            html += '<h4 style="margin:10px 0 4px;font-size:11px;color:#8b949e">Characters (' + et.characterBoxes.length + ')</h4>';
            html += '<table><thead><tr><th>Offset</th><th>Code</th><th>Expected Bounds</th><th>Actual Bounds</th><th>Max \u0394</th></tr></thead><tbody>';
            (et.characterBoxes || []).forEach(function(eb, i) {
                var ab = (at && at.characterBoxes) ? at.characterBoxes[i] : null;
                if (!ab) { html += '<tr class="mismatch"><td>' + eb.utf16Offset + '</td><td class="mono">U+' + eb.codeUnit + '</td><td colspan="3" class="mismatch">Missing</td></tr>'; return; }
                var maxD = 0;
                if (eb.boundsInRootPx && ab.boundsInRootPx) {
                    maxD = Math.max(
                        Math.abs(ab.boundsInRootPx.left - eb.boundsInRootPx.left),
                        Math.abs(ab.boundsInRootPx.top - eb.boundsInRootPx.top),
                        Math.abs(ab.boundsInRootPx.right - eb.boundsInRootPx.right),
                        Math.abs(ab.boundsInRootPx.bottom - eb.boundsInRootPx.bottom));
                }
                var cls3 = maxD < 0.01 ? 'match' : 'mismatch';
                html += '<tr class="' + cls3 + '"><td>' + eb.utf16Offset + '</td><td class="mono">U+' + eb.codeUnit + '</td>' +
                    '<td class="mono">' + (eb.boundsInRootPx ? eb.boundsInRootPx.left.toFixed(1)+','+eb.boundsInRootPx.top.toFixed(1)+'..'+eb.boundsInRootPx.right.toFixed(1)+','+eb.boundsInRootPx.bottom.toFixed(1) : '-') + '</td>' +
                    '<td class="mono">' + (ab.boundsInRootPx ? ab.boundsInRootPx.left.toFixed(1)+','+ab.boundsInRootPx.top.toFixed(1)+'..'+ab.boundsInRootPx.right.toFixed(1)+','+ab.boundsInRootPx.bottom.toFixed(1) : '-') + '</td>' +
                    '<td class="mono ' + cls3 + '">' + maxD.toFixed(2) + '</td></tr>';
            });
            html += '</tbody></table>';
        }
    });

    document.getElementById('textlayout-panel').innerHTML = html;
}

function renderFailures(caseId) {
    var comp = COMPARISONS[caseId];
    if (!comp || !comp.failures || comp.failures.length === 0) {
        document.getElementById('failures-panel').innerHTML = '<div class="empty" style="color:#3fb950">\u2713 No failures</div>';
        return;
    }
    var html = '<div style="padding:4px 0 8px;font-size:12px;color:#8b949e">' + comp.failures.length + ' failure(s) \u2022 Profile: <code>' + (comp.profileId||'').replace(/</g, '&lt;') + '</code>' +
        (comp.pixels ? ' \u2022 Raster: ' + comp.pixels.differentPixels + '/' + comp.pixels.totalPixels + ' px' : '') + '</div>';
    comp.failures.forEach(function(f) { html += '<div class="failure">' + f.replace(/</g, '&lt;') + '</div>'; });
    document.getElementById('failures-panel').innerHTML = html;
}

function renderRaw(caseId) {
    var exp = EXPECTED_LAYOUTS[caseId];
    var act = ACTUAL_LAYOUTS[caseId];
    var comp = COMPARISONS[caseId];
    document.getElementById('raw-panel').innerHTML =
        '<details open><summary style="font-size:12px;color:#8b949e;cursor:pointer">comparison.json</summary><pre style="font-size:11px;max-height:300px;overflow:auto;background:#161b22;padding:8px;border-radius:4px">' + JSON.stringify(comp, null, 2).replace(/</g, '&lt;') + '</pre></details>' +
        '<details style="margin-top:8px"><summary style="font-size:12px;color:#8b949e;cursor:pointer">expected layout.json</summary><pre style="font-size:11px;max-height:300px;overflow:auto;background:#161b22;padding:8px;border-radius:4px">' + JSON.stringify(exp, null, 2).replace(/</g, '&lt;') + '</pre></details>' +
        '<details style="margin-top:8px"><summary style="font-size:12px;color:#8b949e;cursor:pointer">actual layout.json</summary><pre style="font-size:11px;max-height:300px;overflow:auto;background:#161b22;padding:8px;border-radius:4px">' + JSON.stringify(act, null, 2).replace(/</g, '&lt;') + '</pre></details>';
}

document.addEventListener('keydown', function(e) {
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        var items = listEl.querySelectorAll('.case-item');
        var idx = Array.from(items).findIndex(function(el) { return el.dataset.id === activeCaseId; });
        if (e.key === 'ArrowDown') idx = Math.min(idx + 1, items.length - 1);
        else idx = Math.max(idx - 1, 0);
        if (idx >= 0 && items[idx]) { selectCase(items[idx].dataset.id); items[idx].scrollIntoView({block:'nearest'}); }
    }
});
</script>
</body>
</html>"""


def find_latest_run(artifacts_root: Path) -> Path | None:
    """Find the most recently written run directory (by actual/manifest.json mtime)."""
    manifests = list(artifacts_root.rglob("actual/manifest.json"))
    if not manifests:
        return None
    manifests.sort(key=lambda p: p.stat().st_mtime, reverse=True)
    return manifests[0].parent.parent


def collect_data(run_dir: Path) -> dict:
    expected_manifest = json.loads((run_dir / "expected" / "manifest.json").read_text(encoding="utf-8"))
    actual_manifest = json.loads((run_dir / "actual" / "manifest.json").read_text(encoding="utf-8"))

    comparisons = {}
    expected_layouts = {}
    actual_layouts = {}
    image_paths = {}
    families = {}

    for case in actual_manifest.get("cases", []):
        case_id = case["id"]
        families[case_id] = case_id.split("/")[0]
        case_dir = run_dir / "actual" / "cases" / case_id.replace("/", os.sep)
        exp_case_dir = run_dir / "expected" / "cases" / case_id.replace("/", os.sep)

        comp_path = case_dir / "comparison.json"
        if comp_path.exists():
            comparisons[case_id] = json.loads(comp_path.read_text(encoding="utf-8"))

        exp_layout = exp_case_dir / "layout.json"
        if exp_layout.exists():
            expected_layouts[case_id] = json.loads(exp_layout.read_text(encoding="utf-8"))

        act_layout = case_dir / "layout.json"
        if act_layout.exists():
            actual_layouts[case_id] = json.loads(act_layout.read_text(encoding="utf-8"))

        # Relative paths from report.html (which is at run_dir/report.html)
        ip = {}
        exp_img_rel = f"expected/cases/{case_id}/image.png"
        act_img_rel = f"actual/cases/{case_id}/image.png"
        diff_img_rel = f"actual/cases/{case_id}/diff.png"
        exp_copy_rel = f"actual/cases/{case_id}/expected.png"

        if (run_dir / exp_img_rel).exists():
            ip["expected"] = exp_img_rel
            try:
                from struct import unpack
                with open(run_dir / exp_img_rel, "rb") as f:
                    f.seek(16)
                    w = unpack(">I", f.read(4))[0]
                    h = unpack(">I", f.read(4))[0]
                ip["expectedW"] = w
                ip["expectedH"] = h
            except Exception:
                pass
        if (run_dir / act_img_rel).exists():
            ip["actual"] = act_img_rel
            try:
                from struct import unpack
                with open(run_dir / act_img_rel, "rb") as f:
                    f.seek(16)
                    w = unpack(">I", f.read(4))[0]
                    h = unpack(">I", f.read(4))[0]
                ip["actualW"] = w
                ip["actualH"] = h
            except Exception:
                pass
        if (run_dir / diff_img_rel).exists():
            ip["diff"] = diff_img_rel
        elif (run_dir / exp_copy_rel).exists():
            ip["diff"] = exp_copy_rel

        if ip:
            image_paths[case_id] = ip

    return {
        "run_id": run_dir.name,
        "expected_json": json.dumps(expected_manifest, ensure_ascii=False),
        "actual_json": json.dumps(actual_manifest, ensure_ascii=False),
        "comparisons_json": json.dumps(comparisons, ensure_ascii=False),
        "expected_layouts_json": json.dumps(expected_layouts, ensure_ascii=False),
        "actual_layouts_json": json.dumps(actual_layouts, ensure_ascii=False),
        "image_paths_json": json.dumps(image_paths, ensure_ascii=False),
        "case_families_json": json.dumps(families, ensure_ascii=False),
    }


def is_port_available(port: int) -> bool:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        return s.connect_ex(("127.0.0.1", port)) != 0


def main():
    if len(sys.argv) > 1:
        run_dir = Path(sys.argv[1])
    else:
        artifacts = Path(__file__).resolve().parent.parent.parent / "artifacts" / "visual-parity"
        run_dir = find_latest_run(artifacts)
        if run_dir is None:
            print(f"No parity runs found under {artifacts}")
            sys.exit(1)

    if not run_dir.is_dir():
        print(f"Run directory not found: {run_dir}")
        sys.exit(1)

    print(f"Loading data from {run_dir} ...")
    data = collect_data(run_dir)
    html = Template(HTML_TEMPLATE).substitute(data)

    # Write report.html into the run directory so relative image paths resolve
    report_path = run_dir / "report.html"
    report_path.write_text(html, encoding="utf-8")
    print(f"Report written to {report_path}")

    # Serve the run directory over HTTP to avoid file:// CORS issues
    if not is_port_available(PORT):
        print(f"Port {PORT} is already in use. Opening report directly...")
        webbrowser.open(report_path.as_uri())
        return

    os.chdir(str(run_dir))

    class QuietHandler(SimpleHTTPRequestHandler):
        def log_message(self, format, *args):
            pass  # suppress request logs

    server = HTTPServer(("127.0.0.1", PORT), QuietHandler)
    url = f"http://127.0.0.1:{PORT}/report.html"

    print(f"Serving at {url}")
    print("Press Ctrl+C to stop.")

    def serve():
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass

    t = threading.Thread(target=serve, daemon=True)
    t.start()

    webbrowser.open(url)

    try:
        while t.is_alive():
            t.join(1)
    except KeyboardInterrupt:
        print("\nShutting down.")
        server.shutdown()


if __name__ == "__main__":
    main()
