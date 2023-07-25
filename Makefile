project = Chess-Challenge
gui_proj = Chess-Challenge.csproj
uci_proj = Uci.csproj
mode = Release


.PHONY: build
build: build-gui build-uci

.PHONY: build-gui
build-gui:
	dotnet build "$(project)/$(gui_proj)" -c $(mode)
.PHONY: build-uci
build-uci:
	dotnet build "$(project)/$(uci_proj)" -c $(mode)


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
.PHONY: compare
compare: build-uci
	cutechess-cli \
		-engine cmd=dotnet arg=$$(pwd)/$(project)/bin/Release/net6.0/Uci.dll \
		-engine cmd=dotnet arg=$$(pwd)/$(project)/bin/Release/net6.0/Uci.dll initstr=baseline \
		-each proto=uci tc=inf/10+0.0 \
		-sprt elo0=0 elo1=10 alpha=0.05 beta=0.05 \
		-recover -rounds 1024 -games 2 -repeat -concurrency 16 \
		-pgnout "compare-$$(date +%s).pgn" fi -ratinginterval 1


.PHONY: clean
clean:
	rm -r "$(project)/bin" "$(project)/obj"