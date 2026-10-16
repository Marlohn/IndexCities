# Correções da migração da SPEC

Fontes: revisões RM1–RM4, `SPEC-antiga.md` desta pasta e recorte vigente em `docs/SPEC.md`. Trabalho apenas documental, sem regra nova ou alteração de código. `docs/SPEC.md` não foi alterado.

## Alta e Média — cobertura integral

**21 achados: 15 corrigidos e 6 pendentes de decisão.** Cada ID é qualificado pela revisão. Os pendentes tiveram correções independentes aplicadas quando possível; a classificação sem base clara foi preservada e detalhada em `classificacao-pendente.md`, sem inventar inclusão na V1.

| Achado | Gravidade | Status | Tratamento e evidência |
|---|---|---|---|
| RM1-M1 | Média | Pendente de decisão | Corrigida a incoerência “oito” seguida de sete: a introdução distingue a base completa aprovada de oito categorias da lista do recorte V1 e remete à oitava, Suprimentos médicos. A categoria continua na Visão; original e V1 não dão base clara para promovê-la, pois saúde está adiada. |
| RM2-A1 | Alta | Corrigido | Em `prefeitura-financas.md`, retirado “Em aberto” do pacote de empréstimo e restaurada a aprovação humana de 2026-10-12. Mantidos 100.000, 12 meses e 0,3% ao mês. Somente a distribuição das amortizações continua em calibração. |
| RM2-M1 | Média | Corrigido | Restaurada a ressalva original sobre sucessão de outras dívidas pessoais ainda dependente de definição; retirada a afirmação de aplicação automática da regra de Visão na V1. |
| RM2-M2 | Média | Corrigido | Restaurado “até ocorrer uma forma de quitação ou resolução ainda a definir” na dívida de aluguel, sem apagar regras posteriores de desconto salarial e morte já preservadas. |
| RM2-M3 | Média | Pendente de decisão | Removidas todas as anotações do migrador e notas de dúvida equivalentes dos arquivos de regras. Os blocos de investimento/sucessão, multiunidade, vulnerabilidade e participação/orçamento continuam na Visão; suas fronteiras com a V1 carecem de decisão explícita. |
| RM3-A1 | Alta | Corrigido | Restaurado “permanece, até nova decisão” no preço automático de água e energia; tarifa de ônibus separada para Visão. |
| RM3-A2 | Alta | Corrigido | Restaurado “A malha viária deve permitir”; removida a dúvida do título de pontes/viadutos, preservando sua classificação na Visão. |
| RM3-M1 | Média | Corrigido | Tarifa Grátis/Barata/Normal/Cara de ônibus movida para Visão em `servicos.md`, junto da remissão a estacionamento/atrações. Preservada a ressalva de receita só por pagamento efetivo, também aplicável à cobrança V1. |
| RM3-M2 | Média | Corrigido | Bullet integral de emprego único movido de Serviços para População / Em vigor / Trabalho, emprego e renda. Histórico de Serviços agora aponta por link ao destino correto. |
| RM3-M3 | Média | Pendente de decisão | Preservadas as regras de captação/esgoto em Em vigor e poluição/recuperação em Visão. Registrada a dependência e a dúvida de escopo em `classificacao-pendente.md`; não promovidos efeitos de saúde/valor por inferência. |
| RM3-M4 | Média | Pendente de decisão | Variabilidade eólica separada e movida para Em vigor, conforme inclusão explícita na V1; solar permanece na Visão, com condições e ressalvas integrais. Excedente sem armazenamento/exportação permanece na Visão até confirmação de classificação. |
| RM3-M5 | Média | Corrigido | “Na SPEC principal” substituído por link a Construção / Recuperação da interação da V1. Corrigida também a observação do rastreio de mundo-tempo-saves. |
| RM3-M6 | Média | Corrigido | Restaurada a remissão à inadimplência empresarial sem corte, “sujeita às regras reais de insolvência e falência”. |
| RM4-M1 | Média | Corrigido | Em vigor limita o perfil a 1+1 e cruzamentos pertinentes. Texto completo dos dois perfis e cruzamentos com 2+2 preservado na Visão, inclusive custo, materiais, preferência, ciclo fixo e ressalva de validação em interseções vazias. |
| RM4-M2 | Média | Corrigido | Parada física de ônibus e embarque/desembarque movidos para Visão / Transporte público e veículos. A menção a caminhadas até pontos acompanha o sistema adiado; caminhadas até estacionamentos e demais destinos seguem em Em vigor. |
| RM4-M3 | Média | Corrigido | Em vigor conserva tempo real de carga/descarga; regra original completa, incluindo coleta e emergência, preservada na Visão. |
| RM4-M4 | Média | Pendente de decisão | Parto fora de hospital e preferência hospitalar permanecem em Em vigor, como encontrados; a relação entre saúde adiada e nascimentos da V1 não permite reclassificação segura. |
| RM4-M5 | Média | Pendente de decisão | Ausência de seguro-desemprego continua na Visão. O original diz “modelo inicial”; a regra de leitura histórica impede transformar isso automaticamente em inclusão V1. Confirmar o alcance da exclusão e de suas consequências. |
| RM4-M6 | Média | Corrigido | Restaurados “sem garantia de emprego no novo endereço”, a continuidade condicionada no imóvel antigo, o status aprovado da continuidade das entidades e vínculos/salários; links às regras próprias de realocação e emprego. |
| RM4-M7 | Média | Corrigido | Expressão ambígua substituída pelo título exato e link “Prioridade fiscal entre serviços críticos e previdência”, em Prefeitura/finanças. |
| RM4-M8 | Média | Corrigido | Introdução da Cidade viva registrada no histórico: menos de 1 pedestre, nenhum carro, 97% dos minutos vazios e ressalva “dentro das regras completas já aprovadas”. |

## Baixas e higiene documental

- RM1-B1/B2: corrigidos `vigentess` e restaurada a expressão `persistência já aprovada`. B3/B5 continuam no histórico existente; B4 não foi reclassificado por intervenção de forma.
- RM2-B1/B2/B3/B4/B6: retirado travessão órfão; consolidado título duplicado de locação comercial com remissão sem perder seus bullets; regra de falência movida para o título próprio; corrigida maiúscula após ponto e a construção “não altera ... nem permite”. B5 não foi alterado: reabrir “substituição por outros motivos” não é simples correção de forma nesta tarefa.
- RM3: sem mudanças opcionais de produto a partir de Baixas. B5 foi tratado junto de M1; as demais sugestões de estilo, escopo e histórico não foram aplicadas.
- RM4-B1: anotações removidas. B5/B6 tratados junto de M1/M2. B4 consta das pendências. B2/B3 não motivaram alteração de regras nesta rodada.
- Removidas todas as ocorrências de `(verificar se já está no jogo)`, inclusive títulos, bullets e notas equivalentes em `docs/spec/*.md`. Aberturas e calibrações do produto original foram preservadas.
- A observação de RM1 sobre arquivos então ausentes não se confirma no estado integrado: `visao-geral.md`, `transporte-circulacao.md` e `populacao.md` existem; os links do hub resolvem. Nenhuma alteração do hub foi necessária.

## Validação

- Executado `powershell -File scripts/Validate-Docs.ps1`: **nenhum link quebrado fora de `artifacts/`**. Os 11 links apontados estão nos documentos de evidência originais desta pasta (`SPEC-antiga.md` e revisão RM3), cujos caminhos relativos pertenciam à localização de origem; não foram reescritos para alterar evidência. A saída integral fica em `validacao-links.txt`.
- Revisão do diff e busca textual para garantir ausência de dúvidas do migrador em `docs/spec/`; conferência dos destinos e âncoras dos links novos.
- Nenhum teste de jogo é pertinente: não houve alteração de código, comportamento ou assets.
