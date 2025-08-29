import { createSignal, For, createResource } from "solid-js"
import "./App.css"
import type { ProxyConfig } from "./models";

function App() {
  const [error, setError] = createSignal<string | null>(null);

  const [proxyConfigs] = createResource(undefined, async _ => {
    const r = await fetch("/api/configuration");
    const data = await r.json();
    return data.proxyConfig as ProxyConfig;
  });

  // const getStatusBadgeClass = (status: ProxyConfig['status']) => {
  //   switch (status) {
  //     case 'active':
  //       return 'badge-success';
  //     case 'inactive':
  //       return 'badge-warning';
  //     case 'error':
  //       return 'badge-error';
  //     default:
  //       return 'badge-ghost';
  //   }
  // };

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
            onClick={_ => {}}
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
        {/* <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <div class="stat bg-base-100 rounded-lg shadow">
            <div class="stat-title">Total Proxies</div>
            <div class="stat-value text-primary">{proxyConfigs().length}</div>
            <div class="stat-desc">Configured endpoints</div>
          </div>
          <div class="stat bg-base-100 rounded-lg shadow">
            <div class="stat-title">Active</div>
            <div class="stat-value text-success">{proxyConfigs().filter(p => p.status === 'active').length}</div>
            <div class="stat-desc">Running proxies</div>
          </div>
          <div class="stat bg-base-100 rounded-lg shadow">
            <div class="stat-title">Issues</div>
            <div class="stat-value text-error">{proxyConfigs().filter(p => p.status === 'error').length}</div>
            <div class="stat-desc">Requiring attention</div>
          </div>
        </div> */}

        {/* Proxy Configurations Table */}
        <div class="bg-base-100 rounded-lg shadow overflow-hidden">
          <div class="p-6 border-b border-base-300">
            <h2 class="text-lg font-semibold">Running Proxy Configurations</h2>
            <p class="text-sm text-base-content/70 mt-1">Monitor and manage your proxy endpoints</p>
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
                    <th>Target Host</th>
                    <th>Port</th>
                    <th>Target Source</th>
                  </tr>
                </thead>
                <tbody>
                  <For each={proxyConfigs()?.routes}>
                    {(config) => (
                      <tr>
                        <td>
                          <div class="font-medium">{config.routeId}</div>
                          {/* <div class="text-sm text-base-content/70">ID: {config.id}</div> */}
                        </td>
                        <td>
                          <code class="bg-base-200 px-2 py-1 rounded text-sm">{config.order}</code>
                        </td>
                        {/* <td>
                          <span class="badge badge-outline">{config.targetPort}</span>
                        </td>
                        <td>
                          <div class="max-w-xs truncate" title={config.targetSource}>
                            <code class="text-xs">{config.targetSource}</code>
                          </div>
                        </td>
                        <td>
                          <span class={`badge ${getStatusBadgeClass(config.status)}`}>
                            {config.status}
                          </span>
                        </td>
                        <td>
                          <div class="text-sm">
                            {config.lastUpdated.toLocaleDateString()}
                          </div>
                          <div class="text-xs text-base-content/70">
                            {config.lastUpdated.toLocaleTimeString()}
                          </div>
                        </td> */}
                        {/* <td>
                          <div class="dropdown dropdown-end">
                            <div tabindex="0" role="button" class="btn btn-ghost btn-sm">
                              <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z" />
                              </svg>
                            </div>
                            <ul tabindex="0" class="dropdown-content menu p-2 shadow bg-base-100 rounded-box w-32">
                              <li><a>View Details</a></li>
                              <li><a>Edit Config</a></li>
                              <li><a class="text-error">Stop Proxy</a></li>
                            </ul>
                          </div>
                        </td> */}
                      </tr>
                    )}
                  </For>
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Empty State */}
        {proxyConfigs.loading && proxyConfigs()?.routes.length === 0 && !error() && (
          <div class="text-center py-12">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-16 w-16 mx-auto text-base-content/30 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
            </svg>
            <h3 class="text-lg font-medium text-base-content/70 mb-2">No proxy configurations found</h3>
            <p class="text-base-content/50">Get started by adding your first proxy configuration</p>
          </div>
        )}
      </div>
    </div>
  )
}

export default App;
