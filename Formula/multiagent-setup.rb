class MultiagentSetup < Formula
  desc "Scaffold autonomous multi-agent AI workspaces for 12 AI coding assistants"
  homepage "https://github.com/Neftedollar/multiagent-template"
  license "MIT"
  version "1.25.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.25.0/multiagent-setup-1.25.0-osx-arm64.tar.gz"
      sha256 "0ade1d2fad8a1af32f643771f2abba76c57a486422f593c6732fc5e3721762e4"
    else
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.25.0/multiagent-setup-1.25.0-osx-x64.tar.gz"
      sha256 "f6767703eb754a2982cfe7d2b6870b5ce74c1b1cc0f0d461ed6a128f3ef49e21"
    end
  end

  on_linux do
    url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.25.0/multiagent-setup-1.25.0-linux-x64.tar.gz"
    sha256 "1521afeed2d6ce995a8b7a7ebedd0c515020d05b486f5f0449dd645ea2a68226"
  end

  def install
    bin.install "multiagent-setup"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/multiagent-setup --version")
  end
end
