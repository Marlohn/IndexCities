# IndexCities — Mobilidade, serviços e infraestrutura urbana

> **Revisão humana:** PARCIALMENTE REVISADO.  
> **Auditoria:** classificação conservadora com base no estado anterior à reorganização temática, commit `4b97ace2`. o documento mistura conteúdo discutido/confirmado com pesquisa, síntese ou redação da IA ainda não revisada integralmente.
>

> **Status:** exploração ativa; diversos comportamentos já estão parcialmente promovidos para a SPEC — não é fonte de verdade.
>
> Concentra pesquisa e raciocínio sobre serviços urbanos, mobilidade, vias, educação, saúde, infraestrutura técnica, conexão externa e capacidades operacionais. A SPEC continua sendo a autoridade sobre o que já foi decidido.

## Como ler este documento

Este arquivo é material de exploração temática. Quando houver divergência, use:
- `docs/SPEC.md` para o produto desejado;
- `docs/ARCHITECTURE.md` para decisões estruturais de software;
- `AGENTS.md` para regras de trabalho e documentação.

---

## Capacidade hospitalar e equipe médica

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** em exploração; existência de funcionários reais e capacidade real já está decidida na SPEC.

A direção mais coerente é evitar um hospital com capacidade puramente abstrata.

Modelo a investigar:

- cada hospital/unidade possui um quadro real de funcionários;
- médicos, enfermeiros e outros profissionais podem ter funções diferentes;
- capacidade de atendimento depende da equipe disponível, instalações e tempo;
- leitos podem ser um recurso separado da capacidade de consulta;
- turnos podem reduzir a equipe disponível em determinados horários;
- ausência de profissionais pode reduzir capacidade mesmo quando o prédio físico comportaria mais pacientes;
- emergências, consultas e internações podem competir por recursos diferentes.

### Pergunta em aberto

Ainda precisa ser decidido o nível de granularidade da equipe:

1. apenas "médicos" e "enfermeiros";
2. especialidades médicas relevantes;
3. funções hospitalares adicionais;
4. turnos e escalas individuais.

A regra de profundidade continua a mesma: só detalhar quando isso gerar consequência clara para gameplay, capacidade, custo, deslocamento ou decisão do jogador.


---

---

### Decisão transversal: escolha do SIM quando emprego muda de endereço (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou explicitamente a **opção A**, reavaliação automática para trabalhadores **privados e públicos**; a regra de produto foi registrada na SPEC. Os casos de paralisação/continuidade operacional e parâmetros técnicos continuam em exploração.

**Confirmado na SPEC:** quando ocorre mudança efetiva do endereço de trabalho, cada SIM afetado reavalia automaticamente a permanência no emprego, considerando novo trajeto/tempo de viagem, transporte, salário, turno e oportunidades reais. Sem demissão coletiva automática, retenção obrigatória ou seleção manual pelo jogador. **Só é possível continuar se empregador e vaga persistirem**; se o SIM sair, apenas uma vaga que ainda exista volta a ficar disponível para contratação normal. Aplicar a decisão pelos mecanismos normais do mercado de trabalho e em resposta ao evento de mudança, sem varredura contínua por todos os cidadãos. O resultado deve afetar capacidade real de escola, hospital ou outro estabelecimento e produzir deslocamento/efeitos sociais legíveis.

**Continuidade operacional condicionada agora aprovada na SPEC (opção C):** quando uma escola, hospital ou outro serviço municipal for realocado, seu prédio antigo **pode continuar prestando o serviço** enquanto existir, funcionar e não precisar liberar a área da intervenção; se a retirada antecipada for necessária, o atendimento/capacidade daquele endereço é interrompido. A nova estrutura exige obra e condições reais, sem transferência física instantânea nem serviço garantido só porque o destino foi escolhido. **Continuidade institucional agora decidida na SPEC (opção C, 2026-10-08):** escola, hospital e demais serviços municipais permanecem sendo a mesma instituição durante a realocação, sem recriação institucional ou compra privada; capacidade e atendimento só existem onde estrutura, funcionários, acesso e recursos de fato permitirem. Vínculos/vagas seguem válidos apenas se realmente existirem; funcionários reavaliam individualmente quando o endereço de trabalho mudar. **Em aberto, sem autorização para suposição:** tratamento particular de vagas/salários nas paralisações prolongadas, capacidade de atendimento temporário, destino operacional fino dos suprimentos e eventual logística de equipamentos não especificados e critérios finos de transição. A recuperação automática de estoques físicos quando viável e perda do remanescente na retirada inevitável, sem nova indenização ou capacidade fictícia, seguem a SPEC e não exigem novas regras de mercado para serviços públicos. Continuidade institucional não garante operação ininterrupta nem serviço fictício.

## Turnos de trabalho e operação contínua

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** em exploração; suporte a múltiplos turnos já está decidido na SPEC.

A intenção é permitir turnos diferentes para empresas e serviços, inclusive quando houver operação noturna, mas sem transformar escala de funcionários em microgerenciamento manual obrigatório.

Direção a validar:

- cada trabalhador pode ter um horário/turno individual;
- empresas definem janelas de operação e quantidade de vagas por turno;
- serviços críticos podem precisar de cobertura contínua;
- a simulação pode gerar escalas automaticamente a partir das necessidades da empresa/serviço;
- o jogador deve intervir apenas quando houver uma decisão relevante, como ampliar capacidade, mudar horário de funcionamento ou lidar com falta de pessoal;
- horários diferentes devem distribuir ou concentrar tráfego ao longo do dia.

O objetivo é obter consequências reais de horário e escala sem obrigar o jogador a montar manualmente cada escala de trabalho, salvo se isso se provar divertido em protótipo.


---

---

## Transporte público, estacionamento e combustível

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

### Transporte público

Já está decidido que o sistema não ficará restrito a ônibus.

Princípios já definidos:

- veículos de transporte público são entidades reais;
- linhas têm percurso real;
- capacidade importa;
- funcionários/motoristas reais fazem parte da operação;
- outros modais além de ônibus devem existir.

Ainda precisa ser explorado quais modais entram primeiro, por exemplo:

- ônibus;
- vans/micro-ônibus;
- bonde/VLT;
- metrô;
- trem suburbano;
- táxi/transporte sob demanda.

A seleção deve considerar escala da cidade e custo de simulação, não apenas catálogo de features.

### Estacionamento

O estacionamento será parte real da mobilidade.

Questões em aberto:

- estacionamento na rua;
- vagas privadas em residências e empresas;
- estacionamentos públicos;
- custo de estacionamento;
- tempo de procura por vaga;
- efeito da falta de vagas no trânsito e na escolha modal.

O objetivo é manter alto realismo de veículos sem transformar estacionamento em microgestão excessiva para o jogador.

### Cadeia de combustível

Decisão de direção:

produção/refino → distribuição → postos → consumo por veículos.

Pontos a explorar:

- origem da matéria-prima;
- refinaria/fábrica como unidade produtiva;
- transporte por caminhões-tanque;
- estoque real nos postos;
- preço do combustível;
- impacto de falta de combustível na mobilidade e economia;
- consumo diferente por tipo de veículo.

O sistema deve se conectar à logística já decidida, em vez de funcionar como recurso abstrato isolado.

---

---

## Funcionalidades adiadas

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** fora do escopo atual, mas preservadas para reavaliação futura.

- assistência social municipal, incluindo abrigos e programas de apoio — **explicitamente fora do primeiro modelo** após escolha do escopo inicial para população sem moradia; melhorias futuras permanecem em exploração, sem implementação aprovada (ver `households-housing.md`);
- saúde mental e dependência química.

Esses tópicos não devem ser implementados agora. Podem ser revisitados futuramente quando os sistemas básicos de população, saúde, moradia e orçamento já estiverem maduros.


---

---

## Mobilidade ativa e realismo de deslocamento

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

Já está decidido que pedestres e bicicletas existem fisicamente na cidade.

### Pedestres

A intenção é de alto realismo de deslocamento:

- cidadãos caminham fisicamente entre origem e destino;
- viagens multimodais podem incluir trechos a pé;
- cidadãos podem caminhar até estacionamento, ponto de ônibus, estação, comércio, escola e trabalho;
- o caminho de pedestres precisa respeitar calçadas, travessias e acessibilidade viária.

O nível exato de animação, detecção de obstáculos e priorização de travessias ainda precisa ser prototipado.

### Bicicletas

Decidido:

- bicicletas circulam fisicamente;
- bicicleta é um modal real;
- ciclovias fazem parte da infraestrutura.

Adiado para o futuro:

- estacionamento específico para bicicletas.

### Estoque real no comércio

Foi reforçada a regra de que comércio não possui estoque meramente decorativo.

Ela vale para:

- postos de combustível;
- padarias;
- farmácias;
- mercados;
- demais comércios baseados em bens.

**Decidido na SPEC:** compras de bens de consumo pelos cidadãos exigem **visita física a um comércio**, usando os deslocamentos reais de pedestres e/ou veículos, inclusive fora da câmera. O SIM compra apenas produtos de categorias disponíveis no estoque com pagamento real; o estoque diminui e o dinheiro entra no caixa da empresa operadora. A visita de compra é automática, sem teletransporte ou transação invisível que dispense o trajeto.

**Decidido na SPEC para escolha de comércio:** a seleção automática considera **preço, distância/acessibilidade e estoque disponível**. Se a loja não puder suprir a compra, o SIM pode tentar outra opção acessível e deve percorrer a rota real; se nenhuma for viável, a necessidade não é atendida. Isso não exige manter uma loja habitual nem tornar a busca global e contínua. A escolha de outro comércio preserva a concorrência local e os efeitos de ruptura de estoque.

A granularidade inicial por **categorias de produtos** já foi decidida na SPEC; a subdivisão concreta dessas categorias, a política de reposição por estabelecimento, a frequência/agrupamento de compras, os pesos da seleção e o limite de alternativas permanecem para calibração/protótipos, com atenção ao custo em larga escala.

---

---

## Funcionalidades adiadas de mobilidade

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** fora do escopo atual.

- acidentes de trânsito com colisões, feridos e resposta emergencial;
- manutenção mecânica e quebra de veículos;
- estacionamento específico para bicicletas.

Esses sistemas podem ser revisitados depois que mobilidade básica, trânsito, estacionamento de carros e logística estiverem estáveis.


---

---

## Vias, travessias e controle de tráfego

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

### Tipos de via iniciais

Decidido para o escopo atual:

- via urbana comum;
- rodovia.

A via urbana comum terá calçada por padrão.

Novas categorias de via, perfis, larguras, faixas exclusivas e outras variações podem ser adicionadas depois, caso se provem necessárias.

### Estacionamento

Decidido:

- estacionamento na rua ocupa espaço físico real;
- edificações podem oferecer vagas privadas;
- vagas privadas reduzem pressão por estacionamento público.

### Travessias de pedestres

Decidido no escopo atual: pedestres atravessam vias urbanas somente em faixas.

### Semáforos

Decidido no escopo atual: semáforos usam ciclos fixos. Controle adaptativo pode ser reconsiderado futuramente.


---

---

## Função da rodovia e hierarquia viária

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** direção confirmada parcialmente.

A rodovia será tratada como infraestrutura de mobilidade de alta capacidade, não como via de acesso local.

Implicações já definidas:

- maior velocidade operacional;
- sem construção direta de edifícios ao longo da rodovia;
- acesso à cidade feito por conexões apropriadas com a malha urbana;
- capacidade depende do número de faixas;
- rotatórias e cruzamentos fazem parte da rede urbana.

### Razão de design

Separar rodovia de via urbana ajuda a manter uma hierarquia viária compreensível:

- rodovia: deslocamento rápido entre áreas;
- via urbana: acesso local, calçadas, travessias e estacionamento.

Isso também reduz um problema comum em redes viárias: misturar tráfego local com tráfego de passagem na mesma infraestrutura.

### Travessias

Decidido:

- pedestres atravessam somente em faixas.

Ainda pode ser explorado futuramente:

- semáforo para pedestres;
- tempo de espera;
- prioridade de pedestres;
- travessias elevadas ou passarelas.

### Semáforos

Decidido para o escopo inicial:

- ciclos fixos.

Controle adaptativo pode ser reconsiderado no futuro se congestionamentos e gameplay justificarem a complexidade.


---

---

## Granularidade operacional de serviços

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** direção parcialmente fechada.

A simulação deve evitar ações instantâneas quando isso destruir causa e consequência, mas também não precisa reproduzir cada segundo do mundo real.

Direção atual:

- carga/descarga leva tempo;
- coleta de lixo leva tempo;
- atendimento de emergência leva tempo;
- embarque/desembarque leva tempo;
- interior dos prédios permanece abstrato;
- entradas e saídas continuam físicas e observáveis.

A duração exata deve ser configurável e calibrada durante os testes de ritmo do jogo.

### Entregas comerciais locais: fornecedor executa o transporte

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — a opção de **entrega pelo fornecedor** foi aprovada pelo responsável; parametrização, disponibilização de frota inicial e custo de execução seguem pendentes.

**Decidido na SPEC para a primeira implementação integrada:** fazendas, indústrias e demais fornecedores locais organizam suas entregas comerciais com caminhões sob sua operação e motoristas SIM reais. Os veículos carregam produtos na origem e se deslocam fisicamente ao comércio comprador, usando capacidade, vias, trânsito, estacionamento/ponto de descarga e duração de entrega reais. Não transferir o estoque ao comprador antes da chegada; não exigir que cada comércio tenha caminhão próprio para buscar compras nem implementar transportadoras independentes obrigatórias. Ausência de capacidade logística pode atrasar entregas. Os custos de frete permanecem relevantes no preço/custo total já decidido, sem inventar serviço ou cobrança fictícia. Importações usam motoristas/veículos externos conforme a SPEC, e entregas de material para obras preservam suas regras existentes.

**Pendente de calibração/prototipagem:** como empresas obtêm e alocam caminhões/motoristas, limites mínimos do bootstrap, manutenção/frete/faturamento, acúmulo de pedidos, agrupamento e rotas. Avaliar o custo de simulação e os efeitos sobre disponibilidade sem bloquear toda a cadeia por uma burocracia logística excessiva.

### Entregas sem janelas artificiais

Não haverá, por enquanto, obrigação de janelas horárias fixas de entrega para comércio.

A logística deve emergir principalmente de:

- disponibilidade de estoque;
- necessidade de reposição;
- disponibilidade de veículos;
- capacidade de carga;
- distância;
- trânsito;
- fila/capacidade no ponto de carga e descarga.

Se no futuro restrições de horário gerarem gameplay útil, elas podem ser reavaliadas.


---

---

## Filas físicas e horários de funcionamento

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** documentado para futuro; fora do escopo atual.

### Filas físicas em comércio e saúde

No futuro pode ser útil representar filas físicas quando a capacidade de um comércio, hospital ou outro serviço for excedida.

Possíveis consequências:

- espera visível;
- ocupação de calçadas/espaço externo;
- impacto em satisfação;
- atraso em atendimento;
- incentivo para ampliar capacidade.

Não é necessário implementar isso agora.

### Horários de funcionamento

Comércio e serviços podem futuramente ter horários reais de abertura e fechamento.

Possíveis efeitos:

- concentração de viagens em certos horários;
- necessidade de turnos;
- indisponibilidade temporária de serviços;
- redistribuição de demanda.

Também fica fora do escopo atual até a simulação básica de rotina, trabalho e mobilidade estar estável.


---

---

## Eventos temporários e turismo

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** eventos temporários adiados para o futuro; turismo básico já está decidido na SPEC.

No futuro, eventos como shows, feiras e festivais podem ser usados para gerar picos temporários de:

- visitantes;
- demanda por hotéis;
- trânsito;
- transporte público;
- comércio;
- segurança e serviços urbanos.

Esses eventos não entram no escopo atual. Devem ser revisitados depois que turismo, mobilidade, hotelaria e capacidade dos serviços estiverem estáveis.


---

---

## Capacidade educacional

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** estrutura decidida; parâmetros ainda em exploração.

A escola não usará apenas um bônus abstrato de "capacidade".

Já está decidido que:

- professores são cidadãos reais;
- alunos são cidadãos reais;
- quantidade de professores e quantidade de alunos importam;
- falta de vaga/acesso pode deixar jovens fora da escola;
- universidade faz parte do sistema de educação e qualificação.

Ainda precisa ser pesquisado e calibrado:

- razão professor/alunos;
- capacidade física por escola;
- necessidade de salas/turmas explícitas ou agregadas;
- duração das etapas de ensino;
- efeito da distância e transporte no acesso;
- relação entre nível educacional, empregos e salário.

Esses valores devem ser baseados em dados reais ou benchmark do jogo, não escolhidos arbitrariamente.

---

---

## Trabalho pendular bidirecional — opção 4C (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável escolheu C, com preocupação explícita sobre custo técnico e equilíbrio. Direção aprovada na SPEC; modelagem do exterior e dos pagamentos continua aberta para validação.

**Evidência:** o [IBGE — Censo 2022, divulgado em 2025](https://educa.ibge.gov.br/jovens/materias-especiais/23064-censo-2022-como-a-populacao-se-desloca-para-estudar-e-trabalhar.html) identifica **9,3 milhões de pessoas (10,7% dos ocupados) trabalhando em município distinto da residência**, sendo 7,9 milhões com deslocamento três ou mais dias por semana. Essa proporção não é parâmetro aprovado do jogo.

**Benefício para gameplay:** residentes podem trabalhar fora quando há emprego externo realmente acessível, e empresas locais podem contratar trabalhadores externos quando há vagas reais. A entrada e saída pela conexão externa criam tráfego e condições de acesso, sem obrigar o jogador a construir uma segunda cidade. Não contar trabalhadores de fora como moradores nem esconder desemprego dos residentes.

**Custo a medir:** simular todas as casas, empresas e rotinas fora do mapa seria caro e não foi aprovado. Preferir representar vagas/oferta externa de forma leve, com vínculos recorrentes consistentes, tráfego apenas nas vias modeladas e pagamento com origem/destino real. Um pendular não pode ocupar várias vagas incompatíveis nem resolver automaticamente toda falta de pessoal. Contrato externo e saldo entram na contabilidade monetária; pagamentos de/para contrapartes não modeladas seguem a Reserva Global, sem criação de dinheiro. Persistência dos agentes, limites de oferta, regras de decisão e processamento do calendário continuam para exploração técnica antes do código.

## Manutenção municipal recorrente — opção 5B (2026-10-08)

> **Revisão humana desta seção: PARCIALMENTE REVISADO.** O responsável aprovou despesas recorrentes reais e leitura agregada, sem reparos manuais. Fórmulas, serviços/destinatários e tratamento de falta de recursos continuam pendentes.

Expandir rua, energia, água e outras redes traz custos permanentes para o **Caixa da Cidade**, além da obra inicial. A **arrecadação de impostos já especificada** ajuda a financiar os custos, mas nunca sobe automaticamente para cobri-los: gastos excessivos podem gerar déficit, empréstimo e pressão para ajustar impostos. O painel financeiro precisa distinguir manutenção de salários e outras operações, sem cobranças duplicadas.

**Regra anti-ficção:** valor cobrado só sai do Caixa se houver causa e destinatário econômico real: trabalho público que já não tenha sido remunerado, insumos, prestação local ou serviço externo efetivamente devido (com conciliação via Reserva Global). Não criar imposto fictício ou pagamento para ninguém e não inventar falhas de manutenção por rua. Calibrar periodicidade, valor, dependência da capacidade/extent e possíveis consequências de falta de pagamento. Referências: [EPA — asset management](https://www.epa.gov/dwcapacity/about-asset-management) e [Paradox — Economy 2.0](https://www.paradoxinteractive.com/games/cities-skylines-ii/news/dev-diary-economy-part-one), apenas para comparação.

---

## Conexão externa

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** conceito decidido; representação física ainda em exploração.

A conexão externa representa o "mundo fora do mapa".

Ela deve sustentar, conforme os sistemas forem implementados:

- entrada e saída de turistas;
- imigração e emigração;
- transporte rodoviário externo;
- transporte público/interurbano;
- entrada e saída de cargas;
- possíveis outros modais externos no futuro.

A conexão externa evita criar agentes ou mercadorias no meio do mapa sem origem observável.

Ainda precisa ser definido se haverá um único ponto físico, múltiplos pontos por modal ou uma camada lógica comum conectada a diferentes terminais.


---

---

## Infraestrutura de água, esgoto e energia

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

### Água e esgoto

Decidido:

- captação física a partir de rio/lago;
- tratamento de água;
- geração real de esgoto;
- tratamento de esgoto;
- demanda e capacidade importam;
- o jogador não precisa desenhar manualmente encanamentos no escopo atual.

Direção de modelagem:

- edifícios consomem capacidade hídrica;
- infraestrutura construída aumenta capacidade de captação/tratamento;
- excesso de demanda pode gerar falta d'água ou esgoto sem atendimento;
- cobertura pode ser modelada de forma agregada ou por área, sem rede de tubos explícita.

O modelo exato de cobertura ainda precisa ser escolhido.

### Energia

Decidido:

- geração real;
- demanda real;
- capacidade limitada;
- não exigir desenho manual detalhado de linhas/rede elétrica no escopo atual;
- solar e eólica fazem parte das opções.

Ainda em exploração:

- carvão;
- nuclear;
- regras de custo, combustível, poluição e capacidade por tipo de usina.

### Hidrelétrica

**Adiada para o futuro.**

Como hidrelétrica depende fortemente de relevo, curso d'água, barragem e alteração física do terreno, ela deve ser reconsiderada depois que terreno, água e simulação ambiental estiverem mais maduros.

### Poluição

Já está decidido que poluição deve vir de fontes reais da cidade e afetar sistemas reais.

Ainda precisa ser definido:

- tipos de poluição (ar, água, solo, ruído);
- raio/dispersão;
- relação com saúde;
- impacto em valor imobiliário;
- efeito de vento, relevo ou fluxo de água.


---

---

## Resíduos e degradação por falta de água

> **Revisão humana desta seção:** PARCIALMENTE REVISADO — há decisão/discussão humana associada, mas o texto e/ou a pesquisa da IA não foram revisados integralmente.


**Status:** parcialmente decidido.

### Resíduos

Decidido:

- lixo precisa ter destino físico;
- aterro e/ou incineração fazem parte do sistema;
- capacidade de processamento importa;
- reciclagem fica fora do escopo inicial.

Ainda precisa ser definido:

- tipos de instalação iniciais;
- custos operacionais;
- impacto ambiental;
- distância/logística de coleta;
- capacidade por instalação.

### Falta de água

Decidido:

- a resposta não é instantânea;
- primeiro há perda de eficiência;
- após um período sem abastecimento, a atividade pode parar;
- duração e limiares devem ser configuráveis.

Isso permite calibrar o sistema sem transformar uma interrupção momentânea em fechamento imediato.


---

---

## Alcance, influência e escolha de serviços

> **Revisão humana desta seção:** PENDENTE — pesquisa/síntese da IA ainda não revisada pelo responsável.


**Status:** em pesquisa; não é uma regra fechada de produto.

A discussão anterior sobre "não usar círculo de influência" foi fechada cedo demais e fica corrigida aqui.

O que se quer investigar:

- um círculo/área visual pode ser útil para mostrar onde um serviço tende a ter **maior influência ou conveniência**;
- esse círculo não precisa significar um corte absoluto em que cidadãos fora dele ficam proibidos de usar o serviço;
- um cidadão pode aceitar viajar mais longe quando houver motivo, necessidade, qualidade superior, falta de alternativa ou capacidade disponível;
- diferentes sistemas podem precisar de modelos diferentes: hospital, escola, parque, comércio e lazer não necessariamente usam a mesma regra.

Modelos a comparar:

1. **Raio rígido** — simples e barato, mas pouco realista.
2. **Raio como peso/heurística** — proximidade aumenta preferência, sem bloquear destinos mais distantes.
3. **Custo de viagem** — escolha baseada em tempo/distância real de rota.
4. **Modelo híbrido** — raio para UI/diagnóstico + custo de viagem/capacidade para decisão real.

A decisão deve vir de pesquisa, protótipo e legibilidade para o jogador. Não assumir que "fora do círculo ninguém vem".

---
