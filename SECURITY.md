# Security policy

## Supported versions

Security fixes go into the latest release only.

## Reporting a vulnerability

Please do not open a public issue for a vulnerability. Report it privately through GitHub instead: on the
repository's **Security** tab, choose **Report a vulnerability**
([direct link](https://github.com/MykhailoDav/Plugin.Maui.SignaturePad/security/advisories/new)).

Include what an attacker can do, the version you tested, and the smallest code or input that shows the problem.
You should get an answer within a week. Once a fix is released, the advisory is published with credit to you, unless
you prefer to stay anonymous.

## Scope

The parts of the package that handle input an attacker may control:

- `SignatureData.FromJson` reads untrusted text. It validates the whole input before it returns, rejects more than
  10,000 strokes or 100,000 points, and throws `FormatException` for anything it cannot read. Input that makes it hang,
  crash the process or use far more memory than its size suggests is a vulnerability.
- Image and SVG export work on signature data, which can come from such JSON. Export refuses images larger than
  16,384 pixels per side or 64 megapixels in total.
