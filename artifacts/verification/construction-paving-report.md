# Checkpoint de caminhões e pavimentação — 2026-10-16

Entrega ainda em desenvolvimento. Este relato não é aprovação de produto nem revisão independente.

Implementado: carrocerias permanentes por ID/material; frota exterior especializada com limite agregado de viagens preservado; pavimentadora/rolo externos pagos, identidade e deslocamento persistentes; aplicação/acabamento físicos; trechos intermediários exclusivos para obras; abertura comum na conclusão; custos discriminados; migração de cidades anteriores. Arte 170ad66 integrada como 68805ee, sem edição da família de assets pelo autor da simulação.

Verificado no checkpoint:

- `dotnet test ... --filter 'FullyQualifiedName~PavingTests|...Construction|...ConnectedRoadProject|...RoadBootstrap|...Logistics|...FuelDelivery|...Maintenance|...NearbyJunction'`: 76 aprovados, zero falhas.
- `dotnet test tests/IndexCities.Simulation.Tests/IndexCities.Simulation.Tests.csproj --no-build --nologo --filter 'FullyQualifiedName!~CityCycleTests'`: 213 aprovados, zero falhas, 34 s. Log: `construction-paving-final/regression-tests.log`.
- Sete testes próprios incluem bootstrap sem Pátio, acesso, combustível, equipe, verba, concorrência, retorno após cancelar, consumo único e continuação determinística, inclusive save legado parcialmente construído.
- Host compilado com zero erros; aviso preexistente CS8629 em Main.ProblemAlertsQA.cs:198.
- Godot real renderizado em RTX 3050/GL Compatibility: `INDEXCITIES_CONSTRUCTION_PAVING_OK models=final captures=27`, com imagens e JSON reais em `construction-paving-final/`. O termo `models=final` distingue os assets reais do fallback; não afirma aceitação global da entrega.
- Chamada: `Godot_v4.7.2-stable_mono_win64_console.exe --path src/IndexCities.Godot --resolution 1440x960 --position 80,60 -- --construction-paving-smoke --review-save-dir=C:/Users/Marlohn/orca/workspaces/IndexCities/pavimentacao-integracao/artifacts/verification/construction-paving-final`.
- Cenário cadastrado em `-- --qa-list`; logs `engine.log` e `build.log`. Capturas incluem `L-applying.png`, `L-rolling.png`, `straight-unload-Concrete.png`, `straight-unload-Steel.png`, `straight-empty-return-Dump.png` e equivalentes para três carrocerias, reta/L, parada sem combustível e conclusão. Saves pessoais não foram usados.

Pendências conhecidas antes de aceitação:

1. Esclarecimento c4c5206 do coordenador exige descarregar asfalto na pavimentadora já presente. O checkpoint ainda despacha máquinas depois das entregas e descarrega na baia; correção em andamento.
2. Cancelamento após compactação aguarda decisão humana. O checkpoint conserva os nós acabados fechados ao tráfego comum; não atribuir aprovação a esse destino definitivo.
3. Captura `straight-unload-Steel` expôs rolo saindo pelo eixo do canteiro já aberto perto da baia de um caminhão: rever ocupação/saída antes de declarar integração visual validada.
4. A suíte completa anterior foi interrompida para liberar o binário durante edição; a execução final acima exclui o soak CityCycleTests. Os dois erros encontrados em fixtures de cruzamento próximo foram corrigidos: devem selecionar caminhões realmente abastecidos, pois betoneiras não fazem mais viagens de Asfalto.

Calibração atual: aplicação e acabamento por células físicas de 4 m, fases sequenciais para evitar sobrepor pavimentadora e rolo no mesmo canteiro estreito; sem novo comando do jogador ou estoque duplicado. Ainda cabe avaliação do ritmo, da leitura e da estética no jogo pelo coordenador e responsável.

Autor da integração: Codex/GPT-6, variante do modelo não exposta no contexto da API; o coordenador possui a configuração efetiva do despacho. Arte: GPT-6.1-sol xhigh, confirmado no manifesto. Revisão independente DeepSeek V4.1 Flash/Haiku e experiência humana ainda não realizadas nesta worktree.
