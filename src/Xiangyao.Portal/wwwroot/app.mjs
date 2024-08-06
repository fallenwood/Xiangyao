
import { onCleanup, createSignal, createMemo } from "https://esm.sh/solid-js@1.8.1";
import { render } from "https://esm.sh/solid-js@1.8.1/web";
import html from "https://esm.sh/solid-js@1.8.1/html";

async function getConfiguration() {
  const res = await fetch("/api/configuration");
  const json = await res.json();

  console.log("json", json);
  return json;
}

const App = () => {
  const [config, setConfig] = createSignal({});

  createMemo(() => {
    getConfiguration()
      .then(c => {
        console.log("setting config", c);
        console.log(JSON.stringify(c));
        setConfig(c);
      });
  }, []);


  return html`<div>${() => JSON.stringify(config())}</div>`;
  // or
  // return h("div", {}, config);
};

render(App, document.querySelector("#app"));