class MultiagentSetup < Formula
  desc "Scaffold autonomous multi-agent AI workspaces for 12 AI coding assistants"
  homepage "https://github.com/Neftedollar/multiagent-template"
  license "MIT"
  version "1.26.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.26.0/multiagent-setup-1.26.0-osx-arm64.tar.gz"
      sha256 "cbd67cbb0d03b950f87b227af7cbb1a9e871b06384a08abddcdd19c518807434"
    else
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.26.0/multiagent-setup-1.26.0-osx-x64.tar.gz"
      sha256 "c56946228d280a1d28e696a7ec70491ac0eabb1fd5d665f6604bb1c5bc939c4a"
    end
  end

  on_linux do
    url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.26.0/multiagent-setup-1.26.0-linux-x64.tar.gz"
    sha256 "18ac13db7e70a2255deaa9511691d24145fa66279faa481e1a86ac3f70ed5a35"
  end

  def install
    bin.install "multiagent-setup"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/multiagent-setup --version")
  end
end
