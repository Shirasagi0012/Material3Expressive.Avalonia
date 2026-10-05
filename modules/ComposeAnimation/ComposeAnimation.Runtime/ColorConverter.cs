/*
 * Copyright 2019, 2021 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace ComposeAnimation.Runtime;

/// <summary>
/// Converts byte sRGB to [alpha, Oklab L, a, b], preserving Compose's intermediate
/// Color storage (Half channels and 10-bit alpha). The vector transport remains double.
/// </summary>
public sealed class ColorConverter : ITwoWayConverter<Avalonia.Media.Color>
{
    // Fixed Compose Multiplatform Core a9e47e8aa04c95cfe572ab4297c770f3cbfb234e:
    // animation/ColorVectorConverter.kt and ui-graphics Color.kt, Float16.kt,
    // colorspace/{Rgb,Oklab,ColorSpace,ColorSpaces,Illuminant,Adaptation,WhitePoint}.kt,
    // ui-util/MathHelpers.kt. Matrices are column-major; Float operations stay Float.
    private static readonly float[] Bradford =
        [0.8951f, -0.7502f, 0.0389f, 0.2664f, 1.7135f, -0.0685f, -0.1614f, 0.0367f, 1.0296f];
    private static readonly float[] SrgbToXyzD50 = Multiply(
        ChromaticAdaptation(WhitePointXyz(0.31271f, 0.32902f), WhitePointXyz(0.34567f, 0.35850f)),
        ComputeSrgbXyzMatrix());
    private static readonly float[] XyzD50ToSrgb = Inverse(SrgbToXyzD50);
    private static readonly float[] M1 = Multiply(
        [0.8189330101f, 0.0329845436f, 0.0482003018f,
         0.3618667424f, 0.9293118715f, 0.2643662691f,
         -0.1288597137f, 0.0361456387f, 0.6338517070f],
        ChromaticAdaptation(WhitePointXyz(0.34567f, 0.35850f), WhitePointXyz(0.31271f, 0.32902f)));
    private static readonly float[] M2 =
        [0.2104542553f, 1.9779984951f, 0.0259040371f,
         0.7936177850f, -2.4285922050f, 0.7827717662f,
         -0.0040720468f, 0.4505937099f, -0.8086757660f];
    private static readonly float[] InverseM1 = Inverse(M1);
    private static readonly float[] InverseM2 = Inverse(M2);

    public int Dimension => 4;

    public void To(Avalonia.Media.Color value, Span<double> destination)
    {
        var red = SrgbToLinear(value.R / 255f);
        var green = SrgbToLinear(value.G / 255f);
        var blue = SrgbToLinear(value.B / 255f);
        var (x, y, z) = Transform(SrgbToXyzD50, red, green, blue);
        var (l, m, s) = Transform(M1, x, y, z);
        var (lightness, a, b) = Transform(M2, FastCbrt(l), FastCbrt(m), FastCbrt(s));

        // Color.convert(Oklab) constructs a Color before ColorToVector unpacks it.
        destination[0] = QuantizeAlpha(value.A / 255f);
        destination[1] = QuantizeHalf(Math.Clamp(lightness, 0f, 1f));
        destination[2] = QuantizeHalf(Math.Clamp(a, -0.5f, 0.5f));
        destination[3] = QuantizeHalf(Math.Clamp(b, -0.5f, 0.5f));
    }

    public Avalonia.Media.Color From(ReadOnlySpan<double> vector)
    {
        // AnimationVector4D is Float upstream. Constructing the intermediate
        // Oklab Color packs these channels again before converting back to sRGB.
        var alpha = QuantizeAlpha(Math.Clamp((float)vector[0], 0f, 1f));
        var lightness = QuantizeHalf(Math.Clamp((float)vector[1], 0f, 1f));
        var a = QuantizeHalf(Math.Clamp((float)vector[2], -0.5f, 0.5f));
        var b = QuantizeHalf(Math.Clamp((float)vector[3], -0.5f, 0.5f));
        var (l, m, s) = Transform(InverseM2, lightness, a, b);
        var (x, y, z) = Transform(InverseM1, l * l * l, m * m * m, s * s * s);
        var (red, green, blue) = Transform(XyzD50ToSrgb, x, y, z);
        return Avalonia.Media.Color.FromArgb(ToByte(alpha),
            ToByte(LinearToSrgb(red)), ToByte(LinearToSrgb(green)), ToByte(LinearToSrgb(blue)));
    }

    // Rgb's transfer functions use Double internally, with Float boundaries.
    private static float SrgbToLinear(float component)
    {
        var x = Math.Clamp((double)component, 0, 1);
        return (float)(x >= 0.04045
            ? Math.Pow((1 / 1.055) * x + 0.055 / 1.055, 2.4)
            : (1 / 12.92) * x);
    }

    private static float LinearToSrgb(float component)
    {
        var x = (double)component;
        var response = x >= 0.04045 * (1 / 12.92)
            ? (Math.Pow(x, 1 / 2.4) - 0.055 / 1.055) / (1 / 1.055)
            : x / (1 / 12.92);
        return (float)Math.Clamp(response, 0, 1);
    }

    private static byte ToByte(float component) => (byte)(Math.Clamp(component, 0f, 1f) * 255f + 0.5f);
    private static float QuantizeAlpha(float alpha) => (int)(alpha * 1023f + 0.5f) / 1023f;

    private static float FastCbrt(float x)
    {
        var bits = (long)BitConverter.SingleToInt32Bits(x) & 0x1ffffffffL;
        var estimate = BitConverter.Int32BitsToSingle(unchecked(0x2a510554 + (int)(bits / 3)));
        estimate -= (estimate - x / (estimate * estimate)) * (1f / 3f);
        estimate -= (estimate - x / (estimate * estimate)) * (1f / 3f);
        return estimate;
    }

    // Float16.floatToHalf rounds halfway magnitudes up, unlike System.Half's ties-to-even.
    private static float QuantizeHalf(float value)
    {
        var bits = BitConverter.SingleToUInt32Bits(value);
        var sign = (int)(bits >> 31);
        var exponent = (int)((bits >> 23) & 0xff);
        var mantissa = (int)(bits & 0x7fffff);
        var outExponent = 0;
        var outMantissa = 0;
        int half;
        if (exponent == 0xff)
        {
            outExponent = 0x1f;
            outMantissa = mantissa != 0 ? 0x200 : 0;
        }
        else
        {
            exponent = exponent - 127 + 15;
            if (exponent >= 0x1f)
                outExponent = 0x31;
            else if (exponent <= 0)
            {
                if (exponent >= -10)
                {
                    mantissa = (mantissa | 0x800000) >> (1 - exponent);
                    if ((mantissa & 0x1000) != 0) mantissa += 0x2000;
                    outMantissa = mantissa >> 13;
                }
            }
            else
            {
                outExponent = exponent;
                outMantissa = mantissa >> 13;
                if ((mantissa & 0x1000) != 0)
                {
                    half = (((outExponent << 10) | outMantissa) + 1) | (sign << 15);
                    return HalfToFloat(half);
                }
            }
        }
        half = (sign << 15) | (outExponent << 10) | outMantissa;
        return HalfToFloat(half);
    }

    private static float HalfToFloat(int half)
    {
        var bits = half & 0xffff;
        var sign = bits & 0x8000;
        var exponent = (bits >> 10) & 0x1f;
        var mantissa = bits & 0x3ff;
        var outExponent = 0;
        var outMantissa = 0;
        if (exponent == 0)
        {
            if (mantissa != 0)
            {
                var value = BitConverter.Int32BitsToSingle((126 << 23) + mantissa)
                    - BitConverter.Int32BitsToSingle(126 << 23);
                return sign == 0 ? value : -value;
            }
        }
        else
        {
            outMantissa = mantissa << 13;
            if (exponent == 0x1f)
            {
                outExponent = 0xff;
                if (outMantissa != 0) outMantissa |= 0x400000;
            }
            else outExponent = exponent - 15 + 127;
        }
        return BitConverter.Int32BitsToSingle((sign << 16) | (outExponent << 23) | outMantissa);
    }

    private static float[] WhitePointXyz(float x, float y) => [x / y, 1f, (1f - x - y) / y];

    private static float[] ComputeSrgbXyzMatrix()
    {
        // Rgb.computeXYZMatrix(ColorSpaces.SrgbPrimaries, Illuminant.D65).
        const float rx = 0.640f, ry = 0.330f, gx = 0.300f, gy = 0.600f, bx = 0.150f, by = 0.060f;
        const float wx = 0.31271f, wy = 0.32902f;
        var oneRxRy = (1f - rx) / ry;
        var oneGxGy = (1f - gx) / gy;
        var oneBxBy = (1f - bx) / by;
        var oneWxWy = (1f - wx) / wy;
        var rxRy = rx / ry;
        var gxGy = gx / gy;
        var bxBy = bx / by;
        var wxWy = wx / wy;
        var byNumerator = (oneWxWy - oneRxRy) * (gxGy - rxRy) - (wxWy - rxRy) * (oneGxGy - oneRxRy);
        var byDenominator = (oneBxBy - oneRxRy) * (gxGy - rxRy) - (bxBy - rxRy) * (oneGxGy - oneRxRy);
        var bY = byNumerator / byDenominator;
        var gY = (wxWy - rxRy - bY * (bxBy - rxRy)) / (gxGy - rxRy);
        var rY = 1f - gY - bY;
        var rYRy = rY / ry;
        var gYGy = gY / gy;
        var bYBy = bY / by;
        return [rYRy * rx, rY, rYRy * (1f - rx - ry),
            gYGy * gx, gY, gYGy * (1f - gx - gy), bYBy * bx, bY, bYBy * (1f - bx - by)];
    }

    private static (float X, float Y, float Z) Transform(float[] matrix, float x, float y, float z) =>
        (matrix[0] * x + matrix[3] * y + matrix[6] * z,
         matrix[1] * x + matrix[4] * y + matrix[7] * z,
         matrix[2] * x + matrix[5] * y + matrix[8] * z);

    private static float[] Multiply(float[] lhs, float[] rhs) =>
        [lhs[0] * rhs[0] + lhs[3] * rhs[1] + lhs[6] * rhs[2],
         lhs[1] * rhs[0] + lhs[4] * rhs[1] + lhs[7] * rhs[2],
         lhs[2] * rhs[0] + lhs[5] * rhs[1] + lhs[8] * rhs[2],
         lhs[0] * rhs[3] + lhs[3] * rhs[4] + lhs[6] * rhs[5],
         lhs[1] * rhs[3] + lhs[4] * rhs[4] + lhs[7] * rhs[5],
         lhs[2] * rhs[3] + lhs[5] * rhs[4] + lhs[8] * rhs[5],
         lhs[0] * rhs[6] + lhs[3] * rhs[7] + lhs[6] * rhs[8],
         lhs[1] * rhs[6] + lhs[4] * rhs[7] + lhs[7] * rhs[8],
         lhs[2] * rhs[6] + lhs[5] * rhs[7] + lhs[8] * rhs[8]];

    private static float[] Inverse(float[] matrix)
    {
        var a = matrix[0]; var b = matrix[3]; var c = matrix[6];
        var d = matrix[1]; var e = matrix[4]; var f = matrix[7];
        var g = matrix[2]; var h = matrix[5]; var i = matrix[8];
        var xA = e * i - f * h;
        var xB = f * g - d * i;
        var xC = d * h - e * g;
        var determinant = a * xA + b * xB + c * xC;
        return [xA / determinant, xB / determinant, xC / determinant,
            (c * h - b * i) / determinant, (a * i - c * g) / determinant, (b * g - a * h) / determinant,
            (b * f - c * e) / determinant, (c * d - a * f) / determinant, (a * e - b * d) / determinant];
    }

    private static float[] ChromaticAdaptation(float[] sourceWhitePoint, float[] destinationWhitePoint)
    {
        var source = Transform(Bradford, sourceWhitePoint[0], sourceWhitePoint[1], sourceWhitePoint[2]);
        var destination = Transform(Bradford, destinationWhitePoint[0], destinationWhitePoint[1], destinationWhitePoint[2]);
        float[] diagonal = [destination.X / source.X, destination.Y / source.Y, destination.Z / source.Z];
        float[] scaled = [diagonal[0] * Bradford[0], diagonal[1] * Bradford[1], diagonal[2] * Bradford[2],
            diagonal[0] * Bradford[3], diagonal[1] * Bradford[4], diagonal[2] * Bradford[5],
            diagonal[0] * Bradford[6], diagonal[1] * Bradford[7], diagonal[2] * Bradford[8]];
        return Multiply(Inverse(Bradford), scaled);
    }
}
