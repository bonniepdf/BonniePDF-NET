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
  codeword interleaving - originate from the upstream project.

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
</content>
