# IndexCities — automação do laboratório causal

**Objetivo:** continuar autonomamente **uma única branch experimental**, `experimento/motor-causal-10k-80y-autonomo`, rumo à meta da [PR #52](https://github.com/Marlohn/IndexCities/pull/52): 10.000 SIMs simultâneos e individuais, 80 anos gregorianos (29.220 dias) em até 6 horas no desktop de referência, sem perder causalidade. Não cria gameplay aprovado; não altera a V1 nem faz merge na `main`.

## Ativação (uma configuração de credencial é indispensável)

1. Em GitHub **Settings → Secrets and variables → Actions → New repository secret**, cadastrar **`OPENAI_API_KEY`** com chave de API válida e limites de gastos apropriados. Uma assinatura ChatGPT não é uma chave de API; o workflow não consegue criá-la.
2. Em **Actions → IndexCities Causal Lab Autoloop → Run workflow**, iniciar uma vez depois de cadastrar o segredo. Um push inicial do workflow na `main` também tenta o início, mas sem o segredo o mecanismo para informando a dependência.
3. Consultar o ramo [`experimento/motor-causal-10k-80y-autonomo`](https://github.com/Marlohn/IndexCities/tree/experimento/motor-causal-10k-80y-autonomo), os jobs **IndexCities Causal Lab CI** e a PR #52.

## Ciclo e controles

- A IA lê o último CI e a PR #52, continua o **motor já existente** fazendo mudanças **somente** sob `experiments/IndexCities.CausalEngine/**` e a publica **somente na branch isolada**.
- O CI é disparado imediatamente; quando termina, o evento `workflow_run` dispara a próxima análise. **Não depende de timer de hora em hora.** A tarefa diária às 11:17 UTC (08:17 em Brasília) reativa trabalho interrompido pelo limite diário.
- **No máximo 8 execuções de CI por dia UTC**, **um CI por vez**, sem loop quando o agente não fez nenhuma mudança; esse é um limite de segurança/custos, não uma garantia de sucesso.
- O token de push é usado numa etapa do workflow **depois** do agente; o agente não recebe token nem pode editar os workflows. O CI não recebe acesso de escrita ou chave de API.
- CI mínimo roda regressão da V1, o self-test e cenários de **1.000 e 10.000 SIMs por 30 dias** do motor experimental já existente. A execução 10k × 80 anos e a meta de 6h só podem ser afirmadas quando medidas integralmente no hardware correto — CI no GitHub **não comprova essa meta**.
- **Pausar:** em **Actions → IndexCities Causal Lab Autoloop → Disable workflow**. Remover o segredo `OPENAI_API_KEY` também interrompe a execução do agente.
- Chave de API e minutos de GitHub Actions podem gerar custos. Ajustar orçamentos nas respectivas plataformas. O sistema não decide nem incorpora mudança de produto ou arquitetura na `main`.

Este é um laboratório experimental, não um agente autorizado a misturar código e documentação da PR #51 nem a integrar a PR #52 automaticamente.
