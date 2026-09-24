#!/usr/bin/env python3
"""Local test server for the WebGL build.

Unity's WebGL output here uses Gzip compression with Decompression Fallback
off, which unityroom serves correctly on its own. Python's default
http.server does not set Content-Encoding for .gz files, so without this
custom handler a phone/browser would try to run the raw gzip bytes as if
they were uncompressed and fail to load.
"""
import http.server
import functools
import os

PORT = 8420
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Builds", "WebGL")


class GzipAwareHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith(".data.gz"):
            return "application/octet-stream"
        if path.endswith(".wasm.gz"):
            return "application/wasm"
        if path.endswith(".framework.js.gz"):
            return "application/javascript"
        return super().guess_type(path)

    def end_headers(self):
        if self.path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        super().end_headers()


handler = functools.partial(GzipAwareHandler, directory=ROOT)
with http.server.ThreadingHTTPServer(("0.0.0.0", PORT), handler) as httpd:
    print(f"Serving {ROOT} on port {PORT}")
    httpd.serve_forever()
