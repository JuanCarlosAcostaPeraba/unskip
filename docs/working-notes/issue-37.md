# Issue #37 working notes

- Replaced the sidebar's upscaled 32×32 ICO frame with WPF vector geometry matching the original SVG artwork.
- Kept `Assets/unskip.ico` as the executable and Windows window icon.
- Added an STA rendering regression test that confirms the sidebar source is a `DrawingImage`, not a raster `BitmapSource`.
- Preserved the existing dark background, white path, turquoise delivery path, and rounded shape.
