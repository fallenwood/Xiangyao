
import { onCleanup, createSignal } from "https://esm.sh/solid-js@1.8.1";
import { render } from "https://esm.sh/solid-js@1.8.1/web";
import html from "https://esm.sh/solid-js@1.8.1/html";

function getConfiguration() {
    return fetch("/api/configuration")
    .then(res => res.json());
}

const App = () => {
    const [count, setCount] = createSignal(0);
    const [config, setConfig] = createSignal({});

    getConfiguration()
    .then(c => setConfig(c));
    
    return html`<div>${JSON.stringify(config())}</div>`;
    // or
    return h("div", {}, count);
};

render(App, document.querySelector("#app"));