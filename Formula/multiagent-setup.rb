class MultiagentSetup < Formula
  desc "Scaffold autonomous multi-agent AI workspaces for 13 AI coding assistants"
  homepage "https://github.com/Neftedollar/multiagent-template"
  license "MIT"
  version "1.31.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.31.0/multiagent-setup-1.31.0-osx-arm64.tar.gz"
      sha256 "f2dc80e35436de4801f0a6ae1e150bc2fb16a4764d60f96b6b3930e531b0f2f3"
    else
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.31.0/multiagent-setup-1.31.0-osx-x64.tar.gz"
      sha256 "bd7e4bbe73f4beb03c489680a896365ce470e1e3a0f55526f5cf7789efd8bc58"
    end
  end

  on_linux do
    url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.31.0/multiagent-setup-1.31.0-linux-x64.tar.gz"
    sha256 "c43e991a82c1019b1d7706cdfa2d40527b0dcb753cbb17b19aa7611a2744b37b"
  end

  def install
    bin.install "multiagent-setup"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/multiagent-setup --version")
  end
end
