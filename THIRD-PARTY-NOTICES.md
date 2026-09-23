# Third-Party Notices

BonniePDF is built with zero native dependencies, but a small number of
internal components are derived from, or ported from, third-party open-source
software. This file lists each of those components and reproduces the
licence under which the original work was released, as required by the
respective licence terms.

When adding a new component derived from third-party code (e.g. a barcode
symbology, a compression routine, a font-handling helper), append a new
section below following the same format: name the component, state where
the derived files live, link to the upstream project, and reproduce the
upstream licence text in full and unmodified.

This file covers the published `BonniePDF` package. Fonts bundled with the
example projects (the Noto families under `Examples/assets/fonts/`) are not part
of that package; their licence travels with them as `OFL.txt` in the same folder.

---

## QR Code encoder

- **Files:** `BonniePDF/Elements/Shapes/QrCodeEncoder.cs`,
  `BonniePDF/Elements/Shapes/QrSegment.cs`
- **Upstream project:** [Project Nayuki - QR Code generator library](https://www.nayuki.io/page/qr-code-generator-library)
- **Upstream source:** https://github.com/nayuki/QR-Code-generator
- **Licence:** MIT License
- **Nature of use:** `QrCodeEncoder.cs` and `QrSegment.cs` are a C# port of
  Project Nayuki's TypeScript/JavaScript QR Code encoder (ISO/IEC 18004
  Model 2), reshaped to fit BonniePDF's `ElementBase` / `ICanvas` rendering
  conventions. The encoding algorithms - version selection, Reed-Solomon
  error correction, mask penalty scoring, function-pattern drawing, and
  codeword interleaving - originate from the upstream project. The TypeScript
  implementation ports the same upstream source - see the
  [TypeScript notices](https://github.com/BonniePDF/BonniePDF-TypeScript/blob/master/core/THIRD-PARTY-NOTICES.md).

### Licence text

```
Copyright (c) Project Nayuki. (MIT License)
https://www.nayuki.io/page/qr-code-generator-library

Permission is hereby granted, free of charge, to obtain a copy of
this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without
limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject
to the following conditions:
- The above copyright notice and this permission notice shall be
  included in all copies or substantial portions of the Software.
- The Software is provided "as is", without warranty of any kind,
  express or implied, including but not limited to the warranties
  of merchantability, fitness for a particular purpose and
  noninfringement. In no event shall the authors or copyright
  holders be liable for any claim, damages or other liability,
  whether in an action of contract, tort or otherwise, arising from,
  out of or in connection with the Software or the use or other
  dealings in the Software.
```

---

## Data Matrix encoder

- **Files:** `BonniePDF/Elements/Shapes/DataMatrixEncoder.cs`
- **Upstream project:** [datalog/datamatrix-svg](https://github.com/datalog/datamatrix-svg)
- **Licence:** MIT License
- **Nature of use:** `DataMatrixEncoder.cs` is a C# port of the upstream `datamatrix.js`
  Data Matrix (ECC200, ISO/IEC 16022) encoder. The encodation scheme selection
  (ASCII, C40, Text, X12, EDIFACT, Base-256), the Reed-Solomon error correction
  and the module placement originate from the upstream project. The upstream
  SVG/DOM rendering layer is not used: BonniePDF draws the matrix as vector
  rectangles through its own `DataMatrix` element. The TypeScript implementation
  ports the same upstream source - see the
  [TypeScript notices](https://github.com/BonniePDF/BonniePDF-TypeScript/blob/master/core/THIRD-PARTY-NOTICES.md).

### Licence text

The upstream `LICENSE` file:

```
MIT License

Copyright (c) 2020 datalog

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

The upstream source file (`datamatrix.js`) additionally carries the notice:

```
Copyright (c) 2020 Constantine
```

---

## Liberation fonts

- **Files:** `BonniePDF/Typography/Fonts/Liberation*.ttf` (Sans, Serif, Mono -
  Regular/Bold/Italic/BoldItalic), embedded as assembly resources.
- **Upstream project:** [Liberation Fonts](https://github.com/liberationfonts/liberation-fonts)
- **Licence:** SIL Open Font License, Version 1.1 (see
  [`BonniePDF/Typography/Fonts/OFL.txt`](BonniePDF/Typography/Fonts/OFL.txt),
  also packed into the NuGet package).
- **Nature of use:** The Liberation fonts are metric-compatible substitutes for
  the standard-14 PDF fonts (Helvetica/Times/Courier). They are embedded so that
  PDF/A and PDF/UA output can embed real glyph outlines without relying on
  machine-installed fonts. The fonts are redistributed unmodified. This mirrors
  the TypeScript implementation, which embeds the same fonts - see the
  [TypeScript notices](https://github.com/BonniePDF/BonniePDF-TypeScript/blob/master/core/THIRD-PARTY-NOTICES.md).

### Licence text

```
Digitized data copyright (c) 2010 Google Corporation
with Reserved Font Arimo, Tinos and Cousine.

Copyright (c) 2012 Red Hat, Inc.
with Reserved Font Name Liberation.

This Font Software is licensed under the SIL Open Font License, Version 1.1.
The full licence text is reproduced in BonniePDF/Typography/Fonts/OFL.txt and
is available with a FAQ at https://openfontlicense.org
```
