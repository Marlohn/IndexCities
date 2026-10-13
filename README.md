# IndexCities

A primeira versão jogável integrada (V1) do modo Convencional tem implementação executável localmente e está em validação de jogabilidade. A primeira experiência do responsável revelou dificuldades de uso; os testes técnicos não comprovam que a entrega esteja pronta. A simulação em C# se integra à apresentação 3D em Godot, com os dez tipos de construção do catálogo aprovado. Esta entrega roda pelo projeto local; ainda não há executável independente exportado.

A intenção é começar com uma folha em branco: pesquisar, discutir e decidir o produto progressivamente, sem herdar automaticamente arquitetura, requisitos ou decisões de trabalhos anteriores.

## Documentos principais

- [`docs/SPEC.md`](docs/SPEC.md) — **o que já foi decidido para o produto**.
- [`docs/EXPLORATION.md`](docs/EXPLORATION.md) — **pesquisas, ideias, alternativas e decisões ainda em formação**.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — **decisões estruturais de software**.
- [`AGENTS.md`](AGENTS.md) — **como humanos e IAs devem trabalhar e como esses documentos se relacionam**.

A SPEC é o **contrato vivo do jogo**: todo comportamento precisa estar ali documentado **antes do código**, seja como decisão aprovada pelo responsável, seja como **pequena decisão local e reversível tomada pela IA sob delegação expressa**, identificada como pendente de revisão humana. Regras importantes, exceções ou mudanças em comportamentos aprovados continuam exigindo decisão humana. Bugs que restauram regras claras e refatorações que preservam o contrato não precisam reescrever a SPEC. Decisões estruturais relevantes vão para a ARCHITECTURE; pesquisas e propostas não aprovadas ficam na EXPLORATION.

O desenvolvimento segue **Spec-Driven Development (SDD) contínuo** e será feito **em versões menores, jogáveis e integradas**, com testes de gameplay e integração desde cedo. A visão completa aprovada permanece na SPEC, mas não precisa ser entregue toda de uma vez. **O recorte da primeira versão jogável (V1) foi aprovado em 2026-10-12 e está na SPEC**, com [mapa explicativo de dependências](docs/exploration/first-playable-scope.md) e [briefing de assets para trabalho em paralelo](docs/exploration/first-playable-assets.md). Não impor POCs isoladas como fases obrigatórias nem usar simulações fictícias de dinheiro, materiais ou SIMs para fingir funcionamento.

A IA tem **autonomia técnica interna** dentro da arquitetura e autonomia estritamente limitada para pequenos detalhes de produto, desde que **todas as decisões de produto tomadas por IA sejam registradas previamente na SPEC para revisão humana**. Ela não pode decidir sozinha regras importantes, exceções ou consequências sistêmicas. O processo permanece leve, sem microespecificar classes e algoritmos.

## Coordenação da primeira versão

Codex administra a V1 e cuida de documentação, implementação, integração e validação. Claude Opus 5.5 com ultracode produz **somente assets solicitados pelo coordenador**, em pasta delimitada, sem modificar gameplay ou fontes canônicas. Essa divisão foi definida pelo responsável em 2026-10-12 e está no [AGENTS](AGENTS.md#coordenação-da-v1-e-frente-de-assets--2026-10-12).

O [hub de exploração](docs/EXPLORATION.md) permite localizar os temas e seus estados de revisão humana. O [briefing visual](docs/exploration/first-playable-assets.md) é apoio à produção; não aprova medidas, estética, pipeline nem a execução automática de todo o inventário.

## Executar localmente no Windows

Requer SDK .NET 8 ou superior e Godot 4.7.2 **.NET/mono**. Dê dois cliques em [`Jogar.cmd`](Jogar.cmd) para compilar, importar os recursos e abrir o jogo. Os scripts resolvem os caminhos a partir do repositório.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Play.ps1
```

O script compila o host, importa os recursos e inicia o jogo. Use `-GodotPath 'C:\caminho\Godot_mono.exe'` quando a engine estiver fora da pasta local de ferramentas. `-DryRun` mostra o plano sem executar; `-SkipBuild` e `-SkipImport` pulam etapas já realizadas.

Escolha uma construção no catálogo, confira o modelo transparente, sua entrada e o custo, e clique no terreno para encomendar. A Prefeitura libera o catálogo ao encomendar seu projeto. Para ruas, clique no início, confira a ligação e o custo na prévia, e clique no fim para encomendar. A ferramenta permanece pronta para repetir; não há botão separado de confirmação. R gira prédios, Tab alterna o L e Esc ou botão direito cancela a ferramenta. WASD/botão central movem a câmera; roda aproxima; Q/E giram. Espaço pausa; teclas 1/2/3 selecionam tempo real, 15x e 60x; F5 salva. O menu de cidades permite carregar partidas e configurar o autosave. O 1x em tempo real vale também para saves existentes, sem alterar o horário alcançado ou os recursos da cidade.

## Verificação

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Validate-Docs.ps1
```

Na recuperação de 2026-10-13, o VSTest padrão executou a suíte completa: **89 testes aprovados**, incluindo casos parametrizados. Uma sessão anterior apresentou acesso negado ao abrir o processo pai. `scripts/Build.ps1 -LocalAssertions` continua disponível como execução explícita das asserções `Fact`; não substitui a suíte completa com `Theory` nem oculta falhas do runner padrão.

A entrega de construção e ruas passou também num ciclo completo pela interface: fundação, ruas repetidas e em L, catálogo inteiro, operação com 42 prédios e 47 moradores, inspeção, menus, salvamento, criação de outra cidade e retomada da prévia com continuação consistente. Houve produção, compras e salários reais, com oferta monetária preservada. O cenário viário inclui encaixe junto a rua em obra, travessias compartilhadas, T/quatro entradas, Demolir com orçamento revalidado e giro por mouse. [Capturas pareadas, revisão Haiku 5.5, limites e pendências](docs/exploration/v1-human-playtest-2026-10-13.md#evidências-da-recuperação--entrega-de-construção-e-ruas-2026-10-13) estão registrados. A validação anterior inclui 1.044 verificações dos assets e um ensaio de 32 dias simulados; esses resultados técnicos não substituem a avaliação humana da jogabilidade e da estética.

A [referência visual](references/visual/README.md) está preservada no repositório e numa branch remota dedicada. O [pacote de assets V1](src/IndexCities.Godot/Assets/V1/README.md) está integrado, com modelos e imagens dos dez tipos de construção, ruas, vegetação, pedestre e caminhão. A apresentação é inicial e enxuta; a revisão humana da estética e das propostas visuais continua pendente.
