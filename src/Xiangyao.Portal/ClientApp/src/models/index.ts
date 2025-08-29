export interface ConfigurationResponse {
  proxyConfig: ProxyConfig,
}

export interface ProxyConfig {
  revisionId: string;
  routes: Route[];
  clusters: Cluster[];
}

export interface Route {
  routeId: string;
  order: number | undefined;
  clusterId: string | undefined;
}

export interface RouteMatch {
  hosts: string[] | undefined;
  path: string | undefined;
}

export interface Cluster {
  clusterId: string;
  destination: Destination;
  unixSocketPath: string | undefined;
}

export interface Destination {
  address: string;
  health: string | undefined;
  host: string | undefined;
}
