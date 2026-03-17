export function addEventLeftClick(anchor, dotNetHelper) {
    document.getElementById(anchor)?.addEventListener("click", async function (event) {
        event.preventDefault();

        try {
            await dotNetHelper.invokeMethodAsync("OpenAsync", window.innerWidth, window.innerHeight, event.clientX, event.clientY);
        }
        catch {
            // Ignore failures when the Blazor circuit has disconnected.
        }
    });
}

export function addEventRightClick(anchor, dotNetHelper) {
    document.getElementById(anchor)?.addEventListener("contextmenu", async function (event) {
        event.preventDefault();

        try {
            await dotNetHelper.invokeMethodAsync("OpenAsync", window.innerWidth, window.innerHeight, event.clientX, event.clientY);
        }
        catch {
            // Ignore failures when the Blazor circuit has disconnected.
        }
    });
}