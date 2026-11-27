const joined = "/Build/WebGL.data.unityweb";
const parts = [
  "/Build/WebGL.data.unityweb.part0",
  "/Build/WebGL.data.unityweb.part1",
];

export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);
    if (url.pathname !== joined) return env.ASSETS.fetch(request);
    if (request.method !== "GET" && request.method !== "HEAD") {
      return new Response(null, { status: 405, headers: { Allow: "GET, HEAD" } });
    }

    const responses = await Promise.all(
      parts.map((path) => env.ASSETS.fetch(new Request(new URL(path, url), { method: request.method })))
    );
    const failed = responses.find((response) => !response.ok);
    if (failed) {
      responses.forEach((response) => response.body && response.body.cancel());
      return new Response("missing part of " + joined, { status: 502 });
    }

    const etag = '"' + responses.map((response) => (response.headers.get("ETag") || "").replace(/^W\/|"/g, "")).join("-") + '"';
    const headers = new Headers({
      "Content-Type": "application/octet-stream",
      "Cache-Control": "public, max-age=0, must-revalidate",
      ETag: etag,
    });
    const lengths = responses.map((response) => Number(response.headers.get("Content-Length")));
    if (lengths.every((length) => Number.isFinite(length) && length > 0)) {
      headers.set("Content-Length", String(lengths.reduce((sum, length) => sum + length, 0)));
    }

    if (request.headers.get("If-None-Match") === etag) {
      responses.forEach((response) => response.body && response.body.cancel());
      return new Response(null, { status: 304, headers });
    }
    if (request.method === "HEAD") return new Response(null, { status: 200, headers });

    const { readable, writable } = new TransformStream();
    ctx.waitUntil(
      (async () => {
        for (const response of responses) await response.body.pipeTo(writable, { preventClose: true });
        await writable.close();
      })()
    );
    return new Response(readable, { status: 200, headers });
  },
};
