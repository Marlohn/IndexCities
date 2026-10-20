# Oficina — candidato Codex para uso futuro

Asset 3D original de IndexCities. Estética pendente de avaliação humana. Preparado a pedido do responsável para uso futuro; este pacote ainda não está vinculado ao catálogo nem à simulação. A oficina com guincho foi aprovada na SPEC para a entrega em curso durante esta sessão.

**Abrir no Godot:** `Scenes/mechanic_shop_codex.a.tscn`. **Intercâmbio:** `Scenes/mechanic_shop_codex.a.glb`. A receita editável está em `Source/build.gd`, com geometria original em `Source/geometry.gd`. O pacote nativo é independente dos assets V1. Não há geração de malhas durante a partida.

**Ver e girar o modelo:** execute `scripts/PreviewMechanicCodex.ps1` na raiz do repositório. Arraste com o mouse para girar, use a roda para aproximar, R para restaurar e Esc para fechar. `-Lighting studio` troca somente a iluminação da preview; `-CaptureOnly` grava todas as vistas e fecha. Engine testada: Godot 4.7.2 .NET.

## Contrato visual candidato

- Y para cima, fachada em −Z, pivô da raiz no centro do lote, no solo. Escala de autoria: 1 unidade = 1 metro; envelope candidato de 22 × 17 m. Essas medidas não são um footprint aprovado de gameplay.
- Portão, estrutura do elevador, braços do elevador, móveis, acessórios, letreiro e prédio estão separados. O portão tem pivô superior; os braços têm pivô na altura de apoio. Nenhuma animação automática.
- `CandidateAnchors` contém sete referências visuais de portas, soleiras, aproximação, apresentação de veículo e letreiro. Não concedem acesso, capacidade, vagas, atendimento, preço ou emprego. Duas aberturas desenhadas não definem duas vagas funcionais.
- A baia e o elevador estão vazios. Veículos, pessoas, luzes e atividade deverão acompanhar a simulação real na integração futura. Banco, ferramentas e pneus são mobiliário visual, não estoque econômico.
- O letreiro genérico `OFICINA` é substituível e não fixa nome de empresa ou operador. No GLB, o texto vira geometria; na cena nativa continua editável como TextMesh.
- No GLB, as referências `CandidateAnchors` são nós comuns com nomes e transformações preservados; o tipo Marker3D é específico da cena nativa. O ponto `ServiceDoor` fica logo do lado externo da porta.
- Cinco materiais compartilhados entre as partes e um atlas original de cores de 32 × 32 pixels, gerado pela receita. A paleta está embutida no GLB; não há recurso externo de terceiros. Geometria com quinas chanfradas. O manifesto e `Preview/validation.json` registram o custo efetivamente medido.

## Base consultada

Foram lidos o recorte de V1 e a autorização de amostras visuais em `docs/SPEC.md`, os princípios de design, as regras de construção e modelos prontos, a separação de visual/economia e os limites do catálogo em `docs/spec/economia.md`, e a visão futura de quebras/guincho em `docs/spec/transporte-circulacao.md`. A fronteira de assets segue a seção 6 de `docs/ARCHITECTURE.md`.

Na conferência final, a fonte canônica já registrava a aprovação da oficina com guincho para a entrega em curso, em `docs/spec/transporte-circulacao.md`, seção **Quebras, guincho e oficina (2026-10-20)**, com ampliação correspondente no hub e na economia. Essa regra substitui a antiga ideia ainda aberta na exploração. A integração deve seguir a SPEC atual: veículos em serviço são reais e visíveis, e a estrutura visual não determina por si as vagas de atendimento. O espaço aberto, o elevador e o ponto `VehiclePresentationAnchor` ficam disponíveis para apresentar esses veículos; o guincho é um veículo separado, não parte estática do prédio. Este pacote não implementa a operação econômica ou de transporte. Não cria política, controle, cliques ou estados de simulação. O ganho pretendido é reconhecer o prédio ao construir e observar, inclusive sem ler o letreiro.

## Reprodução

Na raiz do repositório, usando o executável Godot .NET instalado:

```powershell
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Candidates/MechanicShopCodex/Source/build.gd
& $engine --headless --path src/IndexCities.Godot --script res://Assets/Candidates/MechanicShopCodex/Source/validate.gd
$evidence = Join-Path (Get-Location) 'artifacts/verification/mechanic-shop-codex'
& $engine --path src/IndexCities.Godot --rendering-method gl_compatibility --resolution 1280x800 --script res://Assets/Candidates/MechanicShopCodex/Source/preview.gd -- ('--out='+$evidence) --lighting=game
```

`--lighting=game` usa os valores diurnos de luz/ambiente do `Main.cs` com GL Compatibility, sem luz de preenchimento. `--lighting=studio`, com Forward+, é uma comparação de acabamento com luz e sombras próprias; não representa a iluminação atual da partida. A preview abre uma janela real e grava imagens por GPU; não abre uma cidade e não acessa saves. Rua, árvores e SIM de escala pertencem somente ao contexto da preview, não ao asset.

O renderizador também aceita `--source=glb` para reimportar e renderizar o arquivo de intercâmbio. As capturas desse caminho usam nomes com `_glb_`, preservando as imagens da cena nativa.

## Verificação e revisão — 2026-10-20

Cena nativa e GLB: **PASS**, 11 meshes, 37.094 triângulos completos, cinco materiais e sete âncoras. O manifesto inclui as dez partes de geometria e o letreiro TextMesh. A paleta serializada foi comparada com o PNG de autoria. Houve sete capturas em GL Compatibility e sete em Forward+, além de uma vista do GLB em cada renderizador. Na comparação de 36 mil pixels por par, o erro médio por canal entre cena nativa e GLB foi 0,0001 na escala 0–255. Os relatórios e capturas estão em `artifacts/verification/mechanic-shop-codex/`.

Revisão independente real pelo **Claude Haiku 5.5** (`claude-haiku-5-5`), em duas rodadas somente leitura pelo Orca. O DeepSeek V4.1 Flash não iniciou: o terminal `dsh` informou ausência de `DEEPSEEK_API_KEY`; foi aplicado o fallback previsto no AGENTS, sem revisão pelo DeepSeek. Run: `run_1861234c58ec`; revisões: `ctx_c5cd0c82bb8a` e `ctx_a33e65ff197e`. A primeira tentativa e o primeiro revisor foram liberados. Ao liberar o revisor final, o Orca o reteve com `user_takeover`; essa decisão foi preservada, sem fechamento forçado. Não restou worker aguardando liberação.

Foram corrigidos: placas/faixas que podiam sugerir vagas funcionais, omissão do texto no manifesto, malha antiga sem uso, referência de porta afastada, placa que ultrapassava o telhado e promessa de correspondência automática da iluminação. A diferença de cor entre nativo/GLB foi reproduzida e corrigida com o atlas de cores; o revisor final confirmou as capturas visualmente iguais e reexecutou o validador com PASS, sem achado bloqueante.

Limitações da revisão: nenhum DCC externo foi usado; o validador não compara automaticamente UV/cor do GLB — essa parte foi conferida no render e na comparação de imagens. O revisor final viu PASS na saída e no JSON, mas não capturou diretamente o código de saída; as execuções do autor terminaram com código 0. Não houve aprovação humana da estética nem teste de gameplay de oficinas.

## Limites

O pacote foi preparado para integração posterior, não integrado ao sistema de oficinas. Colisões, navegação, acesso real, preços, recursos, funcionamento, estados de obra e operação devem seguir a SPEC vigente e a calibração autorizada, sem inventar regras. A entrega ainda exige avaliação humana da estética e validação dentro da versão do jogo que vier a usar a oficina. Não inclui o modelo do guincho, LOD distante nem animação de portão/elevador.
