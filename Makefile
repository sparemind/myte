project = "Chess-Challenge"
gui_proj = "Chess-Challenge.csproj"
uci_proj = "Uci.csproj"
mode = "Release"


.PHONY: build-gui
build-gui:
	dotnet build "$(project)/$(gui_proj)" -c $(mode)
.PHONY: build-uci
build-uci:
	dotnet build "$(project)/$(uci_proj)" -c $(mode)
.PHONY: build
build: build-gui build-uci


.PHONY: run
run:
	cd $(project) && dotnet run --project $(gui_proj) -c $(mode)
.PHONY: uci
uci:
	cd $(project) && dotnet run --project $(uci_proj) -c $(mode)
.PHONY: uci-baseline
uci-baseline:
	cd $(project) && BASELINE=true dotnet run --project $(uci_proj) -c $(mode)


.PHONY: baseline
baseline:
	cd "$(project)}/src" && cp My\ Bot/MyBot.cs Evil\ Bot/EvilBot.cs
