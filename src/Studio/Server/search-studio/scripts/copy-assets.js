const fs = require('fs');
const path = require('path');

const sourceRoot = path.resolve(__dirname, '..', 'src');
const destinationRoot = path.resolve(__dirname, '..', 'lib');
const copiedExtensions = new Set(['.css', '.png']);

if (!fs.existsSync(sourceRoot)) {
    process.exit(0);
}

/**
 * Copies a source file into the emitted lib tree while preserving its relative path.
 *
 * @param {string} sourceFilePath The source file to copy from the TypeScript source tree.
 */
function copyStaticFile(sourceFilePath) {
    // Mirror the source layout under lib so webpack can resolve CSS and image imports from emitted JavaScript.
    const relativePath = path.relative(sourceRoot, sourceFilePath);
    const destinationFilePath = path.resolve(destinationRoot, relativePath);
    fs.mkdirSync(path.dirname(destinationFilePath), {
        recursive: true
    });
    fs.copyFileSync(sourceFilePath, destinationFilePath);
}

/**
 * Walks the source tree and copies the static frontend files that TypeScript does not emit by itself.
 *
 * @param {string} directoryPath The current directory being scanned for static files.
 */
function copyStaticFiles(directoryPath) {
    for (const entry of fs.readdirSync(directoryPath, { withFileTypes: true })) {
        const entryPath = path.resolve(directoryPath, entry.name);

        if (entry.isDirectory()) {
            // Recurse into nested folders so Home-specific styles and assets stay available after build output moves to lib.
            copyStaticFiles(entryPath);
            continue;
        }

        if (!copiedExtensions.has(path.extname(entry.name).toLowerCase())) {
            continue;
        }

        copyStaticFile(entryPath);
    }
}

// Copy the known static frontend files into lib so the Theia bundle can resolve them from emitted JavaScript.
copyStaticFiles(sourceRoot);
