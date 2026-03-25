[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# PrimeReact Theming with SASS

Visit the [official documentation](https://primereact.org/theming/#customtheme) for more information.

<img src="https://upload.wikimedia.org/wikipedia/commons/9/96/Sass_Logo_Color.svg" height="100" alt="SASS Logo" />

## Usage

To compile the CSS once:

```shell
npm install
npm run sass
```

To watch the SASS files for changes and re-compile:

```shell
npm install
npm run sass-watch
```

## Compile and copy CSS files to the PrimeReact repository

Usually you want to update the CSS files in the PrimeReact repository, located in the
`/primereact/public/themes` folder. To do so you can use the following scripts.

These scripts asume that the PrimeReact repository is located next to this repository, so at `../primereact`.
They will compile the CSS files and copy the resulting CSS files to the correct resources folders.

### Unix

```shell
./build.sh
```

### Windows

```shell
build.bat
```

## UKHO Search Studio repository usage

This repository treats `src/Studio/Theme` as the accepted upstream/reference PrimeReact SASS workspace for the first Studio custom theme slice.

Current practical version relationship:

- Studio runtime `primereact` package: `10.9.7`
- upstream/reference `primereact-sass-theme` workspace: `10.8.5`

The workspace should stay read-only in intent for Studio customizations.

- Do not place Studio-owned UKHO/Theia theme source directly in `src/Studio/Theme`.
- Later slices keep Studio-owned custom source under `src/Studio/Server/search-studio/src/browser/primereact-theme/source`.

Current manual bootstrap/build/deploy workflow:

```powershell
Set-Location .\src\Studio\Theme
npm install
npm run build
npm run deploy:studio
npm run verify:studio
```

Equivalent one-line forms from the repository root:

```powershell
npm install --prefix .\src\Studio\Theme
npm run build --prefix .\src\Studio\Theme
npm run deploy:studio --prefix .\src\Studio\Theme
npm run verify:studio --prefix .\src\Studio\Theme
```

The generated Studio-consumed baseline assets are written to:

- `src/Studio/Server/search-studio/src/browser/primereact-theme/generated/ukho-theia-light.css`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/generated/ukho-theia-dark.css`

The Studio-owned source structure that now feeds those generated assets lives under:

- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/shared`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-light`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-dark`

The current deploy step composes the validated Lara light/dark baseline outputs with the Studio-owned UKHO/Theia light/dark SASS source so Studio can load generated local theme content instead of relying only on the stock PrimeReact CDN theme CSS.

Optional manual wrappers:

- Windows: `build.bat`
- Unix-like shells: `build.sh`

