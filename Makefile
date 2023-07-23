
.PHONY: build
build:
	dotnet build


.PHONY: run
run:
	cd Chess-Challenge && dotnet run


.PHONY: baseline
baseline:
	cd Chess-Challenge/src && cp My\ Bot/MyBot.cs Evil\ Bot/EvilBot.cs
