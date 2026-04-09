class MultiagentSetup < Formula
  desc "Scaffold autonomous multi-agent AI workspaces for 12 AI coding assistants"
  homepage "https://github.com/Neftedollar/multiagent-template"
  license "MIT"
  version "1.29.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.29.0/multiagent-setup-1.29.0-osx-arm64.tar.gz"
      sha256 "8cbf2f67ab861536de8665ab523192e5f8aebda71b859a5feb13e0289040add7"
    else
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.29.0/multiagent-setup-1.29.0-osx-x64.tar.gz"
      sha256 "10e2fa69de5dd590db023afedeb8ef470d8af97477de26e89c17bfa7acc1d4e4"
    end
  end

  on_linux do
    url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.29.0/multiagent-setup-1.29.0-linux-x64.tar.gz"
    sha256 "aa161e3b809974e8f5d6de118e63a95307778ca168d856d75dc7634e4d4d7388"
  end

  def install
    bin.install "multiagent-setup"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/multiagent-setup --version")
  end
end
