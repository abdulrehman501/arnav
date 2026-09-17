import time
from pathlib import Path
from pyngrok import ngrok, conf

HERE = Path(__file__).parent
DOMAIN_FILE = HERE / "ngrok_domain.txt"
NGROK_EXE = HERE / "ngrok.exe"


def get_domain():
    if DOMAIN_FILE.exists():
        saved = DOMAIN_FILE.read_text().strip()
        if saved:
            return saved

    print("First-time setup — this only happens once.\n")
    token = input("Paste your ngrok authtoken and press Enter: ").strip()
    ngrok.set_auth_token(token)
    domain = input("Paste your ngrok static domain and press Enter: ").strip()
    # Forgive common paste mistakes: scheme prefixes, trailing dots/slashes.
    domain = domain.removeprefix("https://").removeprefix("http://").strip("./ ")
    DOMAIN_FILE.write_text(domain)
    return domain


def main():
    if NGROK_EXE.exists():
        conf.get_default().ngrok_path = str(NGROK_EXE)

    domain = get_domain()
    # Bind both schemes: some phone networks break TLS for the app, and the app
    # falls back to plain http — that only works if the tunnel answers on http too.
    tunnel = ngrok.connect(8000, "http", domain=domain, schemes=["http", "https"])
    print(f"\nTunnel is online: {tunnel.public_url}")
    print("Leave this window open. Press Ctrl+C to stop.\n")

    try:
        while True:
            time.sleep(60)
    except KeyboardInterrupt:
        pass
    finally:
        ngrok.disconnect(tunnel.public_url)
        ngrok.kill()


if __name__ == "__main__":
    main()
