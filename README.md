# Rad Texture Viewer

This is a cache browser for modern Second Life viewers.

## Disclaimer

Rad Texture Viewer can be used for making edits to skin or clothing textures you have purchased, but it can also be used to steal content. Do not use Rad Texture Viewer to steal content.

## Usage

Select the location of your texture.cache file. This will normall be C:\\Users\\you\\AppData\\Local\\your_viewer\\texturecache\\texture.entries. If you click the button in the top right corner, a file dialog will open, and in the left sidebar of the dialog there will be a "Local" shortcut, which is helpful because AppData is a hidden directory.

The contents of your texture cache should load.

Double-clicking on a texture will open a save dialog for saving that texure to disk.

## Portability

RadTextureViewer uses WPF, which only works on Windows, but RadTextureViewer.Core is a DotNet Standard library using ReactiveUI so theoretically Linux and Mac OS X frontends could be created as well.

## Acknowledgements

Cache parsing code based on [PySLCacheDebugger].

[PySLCacheDebugger]: https://github.com/jspataro791/PySLCacheDebugger