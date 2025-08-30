import { createSignal, For, createResource } from "solid-js"
import "./App.css"
import type { ProxyConfig } from "./models";

function App() {
  const [error, setError] = createSignal<string | null>(null);

  const [proxyConfigs, {mutate: _, refetch: refetchProxyConfigs}] = createResource(0, async _ => {
    try {

    const r = await fetch("/api/configuration");
    const data = await r.json();
    return data.proxyConfig as ProxyConfig;
    } catch (e: any) {
      setError(e.message || "Failed to fetch proxy configurations.");
      throw e;
    }
  });

  return (
    <div class="min-h-screen bg-base-200">
      {/* Header */}
      <div class="navbar bg-base-100 shadow-lg">
        <div class="flex-1">
          <h1 class="text-xl font-bold">Xiangyao Portal</h1>
        </div>
        <div class="flex-none">
          <button
            class={`btn btn-primary ${proxyConfigs.loading ? 'loading' : ''}`}
            onClick={_ => refetchProxyConfigs()}
            disabled={proxyConfigs.loading}
          >
            {proxyConfigs.loading ? 'Refreshing...' : 'Refresh'}
          </button>
        </div>
      </div>

      {/* Main Content */}
      <div class="container mx-auto p-6">
        {error() && (
          <div class="alert alert-error mb-6">
            <svg xmlns="http://www.w3.org/2000/svg" class="stroke-current shrink-0 h-6 w-6" fill="none" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>{error()}</span>
          </div>
        )}

        {/* Statistics Cards */}
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
          <div class="stat bg-base-100 rounded-lg shadow">
            <div class="stat-title">Total Routes</div>
            <div class="stat-value text-primary">{proxyConfigs()?.routes.length}</div>
          </div>
          <div class="stat bg-base-100 rounded-lg shadow">
            <div class="stat-title">Total Clusters</div>
            <div class="stat-value text-primary">{proxyConfigs()?.clusters.length}</div>
          </div>
        </div>

        {/* Proxy Configurations Table */}
        <div class="bg-base-100 rounded-lg shadow overflow-hidden">
          <div class="p-6 border-b border-base-300">
            <h2 class="text-lg font-semibold">Running Proxy Configurations</h2>
          </div>

          {proxyConfigs.loading ? (
            <div class="p-8 text-center">
              <span class="loading loading-spinner loading-lg"></span>
              <p class="mt-4 text-base-content/70">Loading configurations...</p>
            </div>
          ) : (
            <div class="overflow-x-auto">
              <table class="table table-hover w-full">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Hosts</th>
                    <th>Path</th>
                    <th>Cluster</th>
                    <th>Target</th>
                  </tr>
                </thead>
                <tbody>
                  <For each={proxyConfigs()?.routes}>
                    {(config) => (
                      <tr>
                        <td>
                          <div class="font-medium">{config.routeId}</div>
                        </td>
                        <td>
                          <For each={config.routeMatch.hosts}>
                            {(host, idx) => (
                              <>
                                {idx() > 0 && <br />}
                                <div class="font-mono text-xs">{host}</div>
                              </>
                            )}
                          </For>
                        </td>
                        <td>
                          <div class="font-medium">{config.routeMatch.path}</div>
                        </td>
                        <td>
                          <div class="font-medium">{config.clusterId}</div>
                        </td>
                        <td>
                          <div class="max-w-xs truncate" title={config.cluster?.clusterId}>
                            <code class="text-xs">{config.cluster?.destination.address}</code>
                          </div>
                        </td>
                      </tr>
                    )}
                  </For>
                </tbody>
              </table>
            </div>
          )}
        </div>

      </div>
    </div>
  )
}

export default App;
