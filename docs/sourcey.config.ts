import { defineConfig, markdown } from "sourcey";

export default defineConfig({
  name: "ReactiveDAG",
  repo: "https://github.com/richardsmythe/reactive-dag",
  siteUrl: "https://richardsmythe.github.io",
  baseUrl: "/reactive-dag/",
  editBranch: "master",
  editBasePath: "docs",
  theme: {
    preset: "default",
    colors: {
      primary: "#6d28d9",
      light: "#8b5cf6",
      dark: "#4c1d95",
    },
  },
  navbar: {
    links: [
      {
        type: "github",
        href: "https://github.com/richardsmythe/reactive-dag",
      },
    ],
    primary: {
      type: "button",
      label: "NuGet",
      href: "https://www.nuget.org/packages/ReactiveDAG",
    },
  },
  navigation: {
    tabs: [
      {
        tab: "API Reference",
        slug: "",
        source: markdown({
          groups: [
            {
              group: "Overview",
              pages: ["generated/introduction"],
            },
            {
              group: "Engine",
              pages: ["generated/dag-engine", "generated/pipeline-builder"],
            },
            {
              group: "Data model",
              pages: ["generated/cells-and-nodes", "generated/contracts-and-enums"],
            },
          ],
        }),
      },
    ],
  },
});
