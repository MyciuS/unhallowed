# Dev shell for NixOS: provides the .NET SDK and the native libraries
# that Raylib-cs' bundled libraylib.so (GLFW/X11 + OpenGL) loads at runtime.
{ pkgs ? import <nixpkgs> { } }:

let
  runtimeLibs = with pkgs; [
    libGL
    libX11
    libXcursor
    libXrandr
    libXi
    libXinerama
    libXext
    libXrender
    libxkbcommon
    wayland
    alsa-lib
    libpulseaudio
  ];
in
pkgs.mkShell {
  packages = [ pkgs.dotnet-sdk_10 ] ++ runtimeLibs;

  LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath runtimeLibs;
  DOTNET_CLI_TELEMETRY_OPTOUT = "1";
}
