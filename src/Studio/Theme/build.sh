#!/bin/sh
set -eu

# Keep this wrapper manual-on-demand so normal Visual Studio inner-loop runs do not rebuild theme assets implicitly.
SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
cd "$SCRIPT_DIR"

if [ ! -d node_modules ]; then
    # Bootstrap the upstream/reference SASS workspace the first time the wrapper is used on a clone.
    npm install
fi

# Rebuild the upstream/reference CSS output before copying the validated baseline themes into the Studio asset tree.
npm run build

# Deploy the current light and dark baseline CSS files into the Studio-consumed generated asset location.
npm run deploy:studio

# Verify the generated Studio theme files exist so the workflow fails loudly when deployment did not complete.
npm run verify:studio
