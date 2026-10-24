# Proposta definitiva de experimento: IndexCities 10.000 SIMs × 80 anos

> **Tipo:** proposta de implementação e prova de viabilidade, isolada, sob revisão do responsável. **Esta é a direção principal de trabalho, não apenas uma pesquisa nem uma lista de experimentos de outras sessões.** Não altera nem substitui a [SPEC](../SPEC.md), a [ARCHITECTURE](../ARCHITECTURE.md) ou a V1. O motor proposto ainda NÃO demonstrou o desempenho-alvo; não prometer resultado sem executá-lo.
>
> **Autonomia:** o responsável não precisa coordenar outras sessões, pedir E23/E24 nem aguardar PR #51. **Nós construímos e validamos autonomamente a solução completa aqui.** Resultados externos podem ser usados para corrigir hipóteses, mas não bloqueiam implementação, não aprovam desempenho e não substituem nossos testes. A [pesquisa anterior, com E01–E22 e auditorias](simulation-engine-hybrid-causal-research-archive.md) está preservada **apenas como referência histórica**.

## 1. ÚNICO OBJETIVO — o que devemos entregar

Desenvolver um **motor independente que execute uma cidade de pelo menos 10.000 SIMs vivos, simultâneos e individualmente simulados ao longo de 80 anos gregorianos**, incluindo vida familiar, trabalho, deslocamento de pedestres/veículos, trânsito e congestionamento físicos, empresas, produção, compras presenciais, dinheiro e estoques conservados, obras e entregas, energia/água/esgoto/poluição, serviços, saúde, nascimento, crescimento, envelhecimento e morte, além de história individual consultável.

**Critério temporal experimental definido por esta proposta:** simular `2020-01-01 00:00` → `2100-01-01 00:00` (**29.220 dias**) em **até 6 horas reais** num desktop de referência documentado, com **até 4 horas como meta mais ambiciosa**. Os tempos são **alvos do laboratório**, não calibração aprovada do produto; a SPEC exige calendário verdadeiro e todos os acontecimentos reais. Se rodar em 8 horas, o laboratório terá falhado a meta de 6 horas. Se rodar em 6 horas com cidade inativa ou sistemas desligados, **também terá falhado**.

**Orçamento inteiro:** 6h / 29.220 ≈ **0,739 segundos de processamento por dia de jogo**, equivalentes a ~116.880 segundos de jogo por segundo real. Isso precisa incluir todos os domínios e a gravação efetiva de históricos. O objetivo de produto também exige funcionamento com Godot e interface responsiva: sucesso headless isolado é **necessário, mas não suficiente**. Definiremos custo total de renderização/snapshots em teste posterior no jogo, sem simplificar física ou eliminar fatos fora da câmera.

**Cidade representativa, não um truque de benchmark:** 10 mil simultâneos, distribuições de idade/família, residências e empregos; atividade física e econômica consistente, horários de pico, veículos congestionando ruas, cidadãos realmente comprando/consumindo, produção com abastecimento e restrições de infraestrutura, nascimentos e mortes ocorrendo segundo regras aprovadas. Manter 10 mil apenas no início e terminar com 300 não demonstra sustentação da escala. Testar tanto cenário adversarial quanto cidade normal; falência ou despovoamento legítimo são resultados possíveis, **mas não podem mascarar um benchmark que deveria comprovar a capacidade de 10 mil**.

**Não haverá atalhos de produto:** não encurtar dias, não acelerar só a idade, não teletransportar, não concluir emprego/obra/compra sem passagem de tempo e entrega reais, não inventar estoque/dinheiro, não usar um modelo estatístico reduzido para agentes não observados e não descartar fatos de histórico. Compressão computacional só é permitida se preservar identidade, estado, ordem causal e efeitos físicos/econômicos da SPEC. Quando a visão expandida tiver uma regra ainda não definida, registrar a lacuna e continuar a implementação das partes aprovadas; nunca escolher uma mecânica nova e chamá-la de otimização.

## 2. MINHA DECISÃO DE ARQUITETURA — o que efetivamente construir

**Construir um novo núcleo C#/.NET headless, com dados compactos, relógio global único, agenda causal, execução eficiente por intervalos quando exata e física microscópica com índice por faixa e processamento em lote.** Reaproveitar regras puras, geometria, validação, catálogos, cenários e formatos do IndexCities onde a medida mostrar vantagem; reimplementar trechos atuais acoplados ao `Step(2s)` quando necessário. **Não reescrever a V1 durante o laboratório.**

A proposta **não exige** que todo carro ou toda molécula seja processado por eventos. Exige que cada acontecimento devido ocorra e que os passos caros só sejam realizados quando indispensáveis. Se o cálculo de faixa compacta for mais barato que uma previsão e várias invalidações, escolher a faixa compacta.

```text
                        Godot (mais adiante, mesmo estado real)
                                     |
                           comandos e snapshots
                                     |
                   MOTOR EXPERIMENTAL C# / .NET
                       relógio global autoritativo
                                     |
                        agenda causal ordenada
                                     |
       +------------------+----------+------------------------+
       |                  |                                   |
   SIMs/famílias      Economia/obras                    Mobilidade real
   decisões e         dinheiro/estoques                 vias/faixas/corpos
   fatos de vida      produção/serviços                 pedestres/veículos
       |                  |                              filas e conflitos
       +------------------+----------+------------------------+
                                     |
                    RESOLUÇÃO DE RECURSOS E CONFLITOS
                       determinística e transacional
                                     |
                     ESTADO ATUAL + HISTÓRICO EM DISCO
                          + saves/replay/inspeção
```

### 2.1. Como o relógio e os acontecimentos funcionam

- **Um único relógio global.** Eventos têm instante, classe, entidades, versões das dependências e critério determinístico de resolução. Não existe relógio separado por bairro, SIM ou veículo.
- A fila de eventos acorda entidades quando **algo pode mudar seu resultado**, não a cada dois segundos. Eventos coincidentes disputam recursos mediante ordem definida pelas regras de produto; a execução paralela nunca decide desempates acidentalmente.
- Ao processar evento: atualizar/materalizar o estado necessário no instante exato; validar condições; aplicar mudanças em transação causal; emitir fatos duráveis; invalidar previsões atingidas; agendar novos acontecimentos. Se previsão ficou obsoleta, cancelar sem aplicar efeito.
- **Nunca criar um evento por antigo passo de 2 s**. Quando um processo demandar granularidade física, resolvê-lo em bloco compacto; quando tempo/entradas forem estáveis, executar trecho maior sem perder quantização e limiares.
- RNG determinístico por fluxo de entidade/tipo de fato, com contadores persistentes, após verificar compatibilidade das distribuições/regras. Save/resume mantém a mesma sequência e não executa transações duas vezes.

### 2.2. Como a mobilidade realmente funciona

**Modelo físico único; estratégias diferentes para executar o MESMO modelo:**

1. **Caminho-base obrigatório: física compacta por faixas.** Corpos dimensionados, posição real, rota, velocidade/aceleração, distância de frenagem, mudança de faixa, cruzamento, prioridade, reserva e ocupação compartilhada, pedestres, estacionamento, bloqueio, guincho e obras. Arrays por trecho/faixa, acesso direto aos vizinhos, listas compactas de conflitos e buffers reutilizáveis. Em congestionamento severo, processar **somente os grupos causalmente ativos**, com subpassos físicos necessários e sincronização no relógio global. Se não puder demonstrar independência, expandir/sincronizar o grupo.
2. **Acelerador opcional: trajetória com horizonte seguro.** Em movimento livre ou espera com dependência conhecida, usar previsão de posição/ocupação e agendar o primeiro instante potencial de alteração. Entrada de veículo, bloqueio, mudança de semáforo ou via, pedestre, manobra ou efeito externo invalida a trajetória ANTES da interação. Consultar posição exata a qualquer instante; não teletransportar no fim.
3. **Política automática de custo:** medir quando previsão custa menos que física compacta. Quando o trânsito está interligado por filas e cruzamentos, executar o solver detalhado; **não transformar a transição de modo em mudança de leis físicas**.

**Decisão difícil:** o solver de tráfego denso deve ser eficiente sozinho. Se congestionamento exigir recalcular todos os carros indefinidamente, previsão não salvará a arquitetura. Por isso esta implementação ataca estrutura espacial, vizinhos e representação física antes de otimizar despertares. A ordem para atender conflitos, prioridades e reservas segue a SPEC, não a ordem incidental de threads ou da fila.

**Teste obrigatório interno:** demolição de rua com rotas ativas, mudança de sinal, guincho em operação, ultrapassagem, pedestres entrando e saindo do cruzamento, veículo parado por combustível, cadeias longas de frenagem, empate de chegada, mudança de faixa e estacionamento. Comparar **primeira divergência de subpasso e de fato**, não apenas hash no fim do dia. **A descoberta externa de que E22 divergiu é um caso que nossos próprios testes devem capturar**; nenhuma solução E22 precisa estar pronta para implementar este motor.

### 2.3. Como as vidas dos 10 mil SIMs funcionam

Cada cidadão tem identidade estável, família, nascimento/idade, moradia, saúde, fome, patrimônio, conta, trabalho, rotinas, localização e decisões. Agendar turnos, saídas, chegadas, refeições, consultas/atendimentos quando previstos na versão expandida aprovada, decisões sociais e marcos de vida. Notificar somente interessados afetados quando empresa falir, renda mudar, oferta acabar, transporte bloquear, serviço cair ou nascer um filho.

**SIM parado continua envelhecendo, consumindo, acumulando efeitos e podendo morrer:** nenhum desligamento da vida fora de tela. Nenhum grupo de 10 mil pessoas pode virar uma variável estatística. Nascimento/morte real deve modificar população, herança, família, trabalho e serviços no instante correto. O laboratório precisa implementar essas regras que faltam no recorte V1 usando a visão aprovada e documentar cobertura; o atual código V1 não serve como oráculo de eventos geracionais inexistentes nele.

### 2.4. Como a economia e a construção funcionam

Contas e estoques autoritativos; compra só ocorre após deslocamento físico e conferência de vendedor, preço, saldo e disponibilidade. Salário depende de presença efetiva, jornada e empregador; entregas e obras exigem veículo, carga, acesso, trabalho e materiais reais; funcionamento da empresa depende de condições aprovadas. Produção contínua pode acumular delta **já quantizado** por intervalos de entradas invariáveis, interrompendo exatamente em falta de insumo, capacidade, energia, pausa ou chegada/retirada de carga.

**Concorrência concreta:** dois consumidores disputam a última unidade. Um compra e o outro percebe a falta; nunca se duplicam mercadoria e dinheiro. Um único estado autoritativo resolve esses commits mesmo se cálculos de intenção forem paralelos.

### 2.5. Como infraestrutura, poluição e saúde funcionam

**Não basta esperar por notificações externas**, porque vento, energia, poluição, demanda, produção e saúde variam com o próprio tempo. Cada domínio deve fornecer uma descrição testável das leis temporais, fronteiras e mudanças em suas entradas.

- Energia/água/esgoto usam índices de rede, consumidores e capacidade; mudanças de fonte atualizam afetados, não necessariamente todos os prédios em cada passo.
- Vento e poluição exigem **integrais, amostragens e quantização compatíveis com o modelo canônico**. No código V1, poluição é arredondada por passo; calcular uma exponencial única para um dia **não** preserva seus resultados. Se uma composição exata não existir para o trecho, processar os passos realmente necessários e medir.
- Fome, saúde, deterioração, produção e manutenção têm limiares agendados no instante em que cruzam; se água acabar ou energia cair antes, reprogramar o limiar de saúde/trabalho e materializar o efeito anterior corretamente.
- Sistemas acoplados formam uma cadeia causal: **vento → energia → água → produção → alimentação → saúde → morte**. A implementação só passa se o efeito de uma mudança em qualquer elo chegar a todos os dependentes no tempo correto.

### 2.6. Como memória, histórico, Godot e escala funcionam

Estado quente orientado a dados contíguos para física e consultas frequentes; IDs estáveis; índices específicos para acessos/estoques/ocupações. Eventos históricos **append-only** escritos em blocos comprimidos com índices por entidade, tempo e tipo; saves/snapshots versionados e replay. Não carregar décadas de registros na RAM. Exemplo de carga a comprovar: 10.000 pessoas × 15 acontecimentos pessoais/dia × 29.220 dias = **4,383 bilhões de fatos**; o número de eventos **real** depende das regras executadas, não de um contador inventado. Armazenamento e consultas entram no orçamento.

O núcleo executa headless sem Godot, mas expõe **comandos e snapshots consistentes** para a interface real. Godot apresenta a cidade e inspeciona qualquer SIM, sem possuir física autoritativa e sem atualizar cada pessoa por frame. Na velocidade geracional, uma viagem de 20 minutos dura ~10 ms reais; não se promete visualização de cada segundo em 60 FPS enquanto esse avanço ocorre. O modo de observação detalhada deve usar o **mesmo mundo real**, apenas outra velocidade de apresentação.

Começar com executor serial para determinar ordem física/financeira; usar paralelismo em roteamento, consultas, avaliações puras e grupos físicos com independência **provada**. C#/JIT é a implementação inicial, **não uma religião**: se um kernel especializado apresentar limite comprovado, avaliar otimizações de layout/PGO/SIMD e depois módulo nativo ou GPU por lote, contabilizando transferências, ordenação e manutenção. Não exigir GPU antes de medir e não alterar causalidade para aproveitá-la.

## 3. EXECUÇÃO: UM PROGRAMA, UMA RESPONSABILIDADE, SEM DEPENDER DE OUTRAS PRs

**Não vamos criar sucessivos “testes de preparação” como objetivo em si. Vamos implementar o motor acima e executá-lo.** O plano fica em uma única branch de código experimental, criada a partir da `main` vigente quando começarmos o núcleo e evoluída até o teste final. Experimentos de matemática já existentes são ferramentas reutilizáveis, não etapas cuja aprovação depende de terceiros.

Estrutura prevista (ajustável tecnicamente):
```text
experiments/IndexCities.CausalLab/
  Engine/         # relógio global, agenda, commits, RNG, versões, perfil
  City/           # mapa, edifícios, propriedade, índices
  Mobility/       # solver microscópico compacto, trajetórias, pedestres
  People/         # individualidade, família, trabalho, necessidades, vida
  Economy/        # transações, produção, consumo, preços, estoques, logística
  Infrastructure/ # água/energia/esgoto/poluição/manutenção
  History/        # ledger durável, índices por SIM, save/replay
  Scenarios/      # geradores válidos, seeds, stress, geração/estrutura etária
  Tests/          # oráculo, invariantes, primeiros desvios e stress
  Program.cs      # CLI do laboratório, relatório e rastreamento
```

**O código atual pode ser aproveitado ou reescrito conforme qualidade e acoplamento.** A decisão pertence à arquitetura e aos benchmarks, não a uma exigência de preservar classes antigas ou aguardar melhorias nelas. Reaproveitar a mesma lógica aprovada quando poupar risco; evitar carregar o `Step(2s)` como infraestrutura necessária. A migração ao jogo, se aprovada, usará os contratos de comandos/estado/snapshots, nunca fusão automática.

### O que será implementado e verificado na própria execução

Estas são **atividades internas de engenharia de UM laboratório**, não PRs separadas, aprovações do usuário ou motivos para parar e “aguardar retorno”:

1. **Modelo canônico e infraestrutura de diagnóstico:** relógio, prioridade, estado, RNG, save, conservação de recursos e medição; seeds, cena densa, tráfego, histórico, cobertura da SPEC e comparador do primeiro estado/fato divergente. Base de referência independente para física e matemática; conferir regras aprovadas antes de implementar domínio. A V1 serve de regressão onde já implementa o comportamento.
2. **Mobilidade microscópica real em carga:** implementação de faixas, carros, pedestres, cruzamentos e congestionamento, com política de previsão/solver compacto; testar casos adversariais completos **dentro do próprio projeto** e refutar qualquer otimização que produza divergência não autorizada. O motor de eventos não fica aguardando resultados de E22.
3. **Fatia funcional completa e integrada, não estatística:** ruas/casa/empresa reais, emprego, deslocamento presencial, compra, salário, produção, frete, combustível, obras, serviços, poluição, fome e saúde no mesmo relógio. Acompanhar estados de um Sim, saldo, entrega e consumo desde causa até consequência.
4. **Demografia e persistência longa:** nascimento, idade, crescimento, morte, migração legítima, herança e famílias; histórico durável e consultável por entidade e período; snapshots e replays com mesmo estado e efeitos depois de recarregar.
5. **Escala representativa e correção de gargalos:** 1k → 5k → 10k **Sims simultâneos**, cidade real em dias de pico, semanas e anos. Perfil de CPU, memória, GC, disco, quantidade de eventos reais, invalidadores, filas, carros ativos e transações. Corrigir gargalos estruturais no mesmo código, repetir carga adversarial; executar o horizonte completo de 80 anos e medir o resultado sem extrapolar de janelas curtas. Se inviável nos limites estudados, explicar qual custo é inevitável e qual alternativa mensurável resta — sem declarar que atingiu meta.
6. **Integração final de produto:** uma vez demonstrado headless, conectar o núcleo à apresentação Godot **no ramo experimental, sem tocar a main**, medir snapshots, interface, renderização, save/load, cadência e responsividade. O jogo inteiro é a prova final, não a fila de eventos nem um benchmark de kernel.

**Não entregar a execução pela metade chamando-a de concluída.** Uma diferença entre dois algoritmos deve ser reproduzida e investigada. O projeto segue, no mesmo trabalho, até **uma conclusão objetiva sobre o objetivo completo**, seja sucesso documentado ou reprovação fundamentada por limites medidos. Os relatórios parciais ajudam a engenharia mas não exigem que o usuário dê ordens para “continuar”.

## 4. TESTE FINAL — o único critério de vitória

A CLI headless da solução precisa aceitar seed, cenário, instante inicial/final, modo detalhado de auditoria, output e opção de retomar save. Uma execução final precisa produzir **um relatório reprodutível**, com commit, CPU/RAM/disco/OS/.NET, seed, cenário válido, 10k SIMs simultâneos, **29.220 dias realmente processados**, wall-clock, picos de custo, série de população/empregos/produção, trajetórias e veículos, recursos e dinheiro conservados, falhas, eventos por pessoa, GB de logs, espaço ocupado, primeiro desvio e custos de save/consulta.

Testes que obrigatoriamente fazem parte **da nossa implementação**, não de outra sessão:

| Caso | O que precisa passar |
| --- | --- |
| **Trânsito normal e engarrafado** | Geometria real, mesmo resultado físico conforme oráculo, sem atravessamentos, sem bloqueios ignorados e com custo de pico medido |
| **Trânsito adversarial** | Demolição, mudança de preferência, guincho, pedestres, rota, combustível, bloqueio e filas; dependências corretas |
| **Economia em disputa** | A última unidade só pode ser vendida uma vez; salários, entregas, estoques e dinheiro têm origem real |
| **Infraestrutura temporal** | Vento, energia, água, poluição e saúde mudam no instante correto, incluindo falha de serviço no meio de um intervalo |
| **Família e décadas** | SIMs nascem, crescem, envelhecem e morrem; pessoas, vínculos, trabalho e patrimônio são reais |
| **Save e história** | Um SIM pode ser investigado depois de décadas; recarregar não duplica pagamentos, compromissos nem trajetórias |
| **Carga e duração** | Cidade realmente ativa com >=10k SIMs; completar 80 anos no hardware fixado e medir tempo integral |
| **Godot** | Jogar/inspecionar sem perder causalidade por câmera ou velocidade; interface não corrompe o motor |

**Veredito permitido:** **ATINGIU** (cobre a matriz, execução final completa e prazo cumprido), **NÃO ATINGIU** (qualquer requisito ou tempo falhou), ou **INCONCLUSIVO** (teste incompleto/hardware não validado). Ganhar 30% numa rua, 100× em fome ou usar um benchmark que desabilitou economia nunca recebe status **ATINGIU**.

Máquina inicial a fixar: desktop de aproximadamente 8 núcleos modernos, 32 GB RAM, SSD NVMe. GitHub Actions compartilhado executa regressões e testes menores, mas **não é prova comparável de limite de 6h no desktop**. Tempo de preparação de fixture e volume de dados devem ser expostos; não esconder trabalho caro em pré-processamento gratuito.

## 5. RISCOS REAIS E COMO O PRÓPRIO PROJETO LIDA COM ELES

- **Trânsito denso inviabiliza previsões:** usar física de faixas compacta como fundamento; não depender de ganho de “carros dormindo” e medir custo por interação. Se fila compacta ainda consumir orçamento excessivo, investigar kernel nativo/vetorização com equivalência explícita.
- **Explosão de eventos e invalidações:** assinaturas versionadas específicas, cancelamento barato, índices locais, prevenção de ciclos instantâneos e acordar fatos devidos; medir custo total, não cache-hit.
- **Composição temporal altera estados:** manter aritmética quantizada por passo quando aplicável, calcular limiares, preservar instante de consumo/produção/morte. O P0 já demonstrou diferenças quando atalhos ingênuos são usados; não repetir o erro.
- **Gargalo sai do trânsito e vai para energia, saúde ou história:** medição integral por domínio; arquitetura de estado quente e processamento incremental em todas as áreas, não só veículos.
- **Custo inevitável de bilhões de fatos:** persistência em blocos com backpressure, reconstrução e índices; qualquer compressão deve manter acontecimento consultável. Medir volume e taxa de disco reais.
- **Novo motor vira outro jogo incompatível:** mesmas regras da SPEC, contratos de integração e matriz de cobertura; nenhuma mudança de física perceptível escondida. Biblioteca atual é referência parcial da V1, nunca prova para funcionalidades futuras inexistentes.
- **Interação visual em aceleração extrema:** snapshots consistentes e velocidades adequadas, sem desligar simulação dos invisíveis.
- **A meta pode ser fisicamente inviável no hardware de referência:** nenhum algoritmo reduz a zero o custo de interações verdadeiras. Se resultados medidos demonstrarem impossibilidade sob limites fixos, entregar evidências, alternativas de hardware/tempo e a decisão de produto pendente — nunca simular vitória.

## 6. RESPONSABILIDADE E STATUS

**Uma única proposta, um único resultado buscado, uma implementação experimental autossuficiente.** Não haverá “dependência da outra sessão”. A PR #51 e pesquisas antigas são bibliografia e fonte de casos adversariais. Os experimentos P0 anteriores, [documentados na PR #53](https://github.com/Marlohn/IndexCities/pull/53), validaram apenas acumuladores e casos locais; **não constituem implementação desta proposta** e não precisam ser “aprovados” para que o motor seja construído. Os números registrados continuam úteis mas são insuficientes para afirmar progresso no objetivo principal.

**Situação real nesta revisão:** documento central corrigido e pesquisas preservadas no [arquivo histórico](simulation-engine-hybrid-causal-research-archive.md). **O núcleo completo de 10k/80 anos ainda não foi construído nem validado.** A próxima ação da equipe responsável pelo experimento é **implementar esse motor único** no ramo experimental, executando internamente toda a matriz de validação. Nenhuma alteração automática na `main`, nenhuma adoção da nova arquitetura antes de prova e aprovação apropriada na ARCHITECTURE. 
