class MultiagentSetup < Formula
  desc "Scaffold autonomous multi-agent AI workspaces for 12 AI coding assistants"
  homepage "https://github.com/Neftedollar/multiagent-template"
  license "MIT"
  version "1.27.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.27.0/multiagent-setup-1.27.0-osx-arm64.tar.gz"
      sha256 "0b963f4c4330d57c403c9b7823ab20cba76e5d7f128be0a64c621f1b6ec85754"
    else
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.27.0/multiagent-setup-1.27.0-osx-x64.tar.gz"
      sha256 "80d0a6b2bceda7a122cb92f99d7b63c8f22c93cefcf4cc62bb9cc24f1a2d5037"
    end
  end

  on_linux do
    url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.27.0/multiagent-setup-1.27.0-linux-x64.tar.gz"
    sha256 "bbfd8943a0dc5447563a77a0e9532886bdabb6428076539c0c22838112f61455"
  end

  def install
    bin.install "multiagent-setup"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/multiagent-setup --version")
  end
end
