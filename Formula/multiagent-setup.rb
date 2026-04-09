class MultiagentSetup < Formula
  desc "Scaffold autonomous multi-agent AI workspaces for 12 AI coding assistants"
  homepage "https://github.com/Neftedollar/multiagent-template"
  license "MIT"
  version "1.30.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.30.0/multiagent-setup-1.30.0-osx-arm64.tar.gz"
      sha256 "f9438ad6003a6a0f4e57fbb9f9429307a564b95f793745b92fd9271ae45e00fe"
    else
      url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.30.0/multiagent-setup-1.30.0-osx-x64.tar.gz"
      sha256 "a031aa050b0f874ddc7c587ff73dbd8e0a492ded8779ef2000b99ebdee8087ba"
    end
  end

  on_linux do
    url "https://github.com/Neftedollar/multiagent-template/releases/download/v1.30.0/multiagent-setup-1.30.0-linux-x64.tar.gz"
    sha256 "90aa211e4c9e35c930caf6863d9a405d323ce938a18075c2a350d72e6e64bc2a"
  end

  def install
    bin.install "multiagent-setup"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/multiagent-setup --version")
  end
end
