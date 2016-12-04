UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S),Linux)
	ifndef KSPDIR
		KSPDIR := ${HOME}/.local/share/Steam/SteamApps/common/Kerbal Space Program
	endif
	ifndef MANAGED
		MANAGED := ${KSPDIR}/KSP_Data/Managed/
	endif
endif
ifeq ($(UNAME_S),Darwin)
	ifndef KSPDIR
		KSPDIR  := ${HOME}/Library/Application Support/Steam/steamapps/common/Kerbal Space Program
	endif
	ifndef MANAGED
		MANAGED := ${KSPDIR}/KSP.app/Contents/Resources/Data/Managed/
	endif
endif

SOURCEFILES := $(shell find Source -name "*.cs")

MCS ?= mcs

all: build

info:
	@echo "mcs:      ${MCS}"
	@echo "KSP Data: ${KSPDIR}"
	@echo "Managed:  ${MANAGED}"

build: MechJim.dll

MechJim.dll: $(SOURCEFILES)
	${MCS} -t:library -lib:"${MANAGED}" -debug \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine,UnityEngine.UI \
		-out:$@ ${SOURCEFILES}

install: build
	mkdir -p "${KSPDIR}"/GameData/MechJim/Plugins
	cp MechJim.dll "${KSPDIR}"/GameData/MechJim/Plugins
	cp -r Icons "${KSPDIR}"/GameData/MechJim

uninstall: build
	rm -rf "${KSPDIR}"/GameData/MechJim

clean:
	rm -f MechJim.dll
