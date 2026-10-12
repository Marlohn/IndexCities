# Lições de uma experiência anterior de prototipação

> **Revisão humana: PENDENTE.** Síntese informal produzida por IA a partir de registros históricos e análise estática, ainda não revisada integralmente pelo responsável. Os problemas descritos não foram reproduzidos no IndexCities.
>
> **É um relato de aprendizado, não uma especificação, plano de implementação ou lista de tarefas.** Não remete a repositórios, arquivos, commits, issues ou assets de experiências anteriores. A fonte das decisões do IndexCities continua sendo a [SPEC](../SPEC.md), e a forma de trabalhar está em [AGENTS](../../AGENTS.md).

Uma experiência anterior de desenvolver um jogo de construção e simulação urbana trouxe experimentos interessantes, mas **não chegou ao resultado desejado**. Em retrospecto, alguns tropeços valem ser lembrados. Não para reproduzir o que foi feito, e sim para reconhecer riscos comuns quando aparecerem no trabalho atual.

Estas notas não são uma avaliação de que *tudo* deu errado: alguns problemas eram de versões específicas, outros foram observados apenas em testes ou investigações, e algumas soluções chegaram a ser experimentadas sem validação final. Não há aqui uma biblioteca de soluções prontas.

## As lições que ficaram

### Clicar precisa fazer o que o jogador espera

Houve situação em que o clique sobre uma construção selecionava o terreno atrás dela. Também apareceu uma prévia de posicionamento que nem sempre verificava todas as condições necessárias para executar a ação.

**O aprendizado:** interface de construção não é só visual. Seleção, prévia, cancelamento, confirmação e mensagens de impedimento precisam corresponder ao estado real da cidade. É especialmente importante quando quem joga depende mais de imagens do que de leitura.

### Uma mudança local pode quebrar algo distante

Um caso histórico combinava remoção de construção e via e deixava um veículo numa posição que já não fazia sentido. Verificar cada operação isolada não foi suficiente para perceber o problema.

**O aprendizado:** sistemas interligados podem ficar incoerentes depois de duas ações perfeitamente comuns. Vale pensar em situações combinadas — mudanças de vias, ocupantes, contratos, obras e cargas em trânsito — e observar se a cidade preserva identidade, localização e propriedade dos envolvidos.

### Um prédio existente não significa um serviço funcionando

Uma cidade podia ter equipamentos de atendimento e, ainda assim, pessoas sem acesso real ao serviço. Indicadores agregados e mapas de carência ajudavam a revelar a diferença; por outro lado, amostras pequenas ou enviesadas podiam induzir conclusões erradas.

**O aprendizado:** quantidade de edifícios não é sinônimo de capacidade utilizada, vaga disponível, acesso possível ou atendimento realizado. Um relatório útil deveria conseguir distinguir essas situações, sempre conforme as regras efetivamente definidas para o jogo.

### Estatísticas podem estar matematicamente corretas e conceitualmente erradas

Uma métrica de saúde foi distorcida porque contava acontecimentos de um grupo de pessoas, mas dividia por uma população de referência diferente. O problema não era a conta em si, mas *quem* estava sendo contado.

**O aprendizado:** antes de confiar numa taxa, esclarecer o período, a população elegível, o numerador, o denominador e os casos em que ainda faltam dados para uma conclusão.

### Repetível não é necessariamente natural

Uma rotina aleatória reproduzível fazia certas escolhas de uma pessoa se repetirem dia após dia. Era estável para depuração, mas podia produzir uma vida pouco convincente. O comportamento histórico foi posteriormente ajustado naquela experiência; não é um defeito que deva ser presumido como atual.

**O aprendizado:** características duradouras e decisões que variam com o dia são coisas diferentes. Determinismo é valioso, mas a rotina dos cidadãos precisa continuar coerente com tempo, necessidades, recursos e oportunidades reais.

### Renderizar bem não garante simular bem — nem o contrário

A experiência mostrou que uma cidade grande pode sobrecarregar a renderização com muitos objetos fora da área útil da câmera. Também mostrou a importância de distinguir a velocidade solicitada pelo jogador da velocidade que a simulação realmente consegue entregar.

**O aprendizado:** desempenho visual, avanço efetivo do relógio e processamento dos acontecimentos são medidas diferentes. Uma tela fluida não prova que o mundo está avançando corretamente, e uma otimização visual não substitui medição em cenários representativos.

### Um teste só prova alguma coisa se o cenário realmente aconteceu

Uma alteração em outro sistema mudou a evolução de uma cidade de teste e impediu que a situação esperada surgisse. O teste deixava de avaliar exatamente aquilo que pretendia.

**O aprendizado:** antes de interpretar o resultado, verificar se as condições necessárias estavam presentes. Isso evita tanto falsos alarmes quanto testes que passam sem exercitar o comportamento relevante.

### Procedência e aparência dos recursos visuais importam

Experimentar conjuntos diferentes de modelos e materiais trouxe dificuldades de escala, identidade visual, variedade e proveniência. Também ficou claro que testes automáticos de carregamento não demonstram, sozinhos, se o resultado é legível ou agradável.

**O aprendizado:** qualidade visual exige julgamento no jogo real, em diferentes distâncias e densidades. Direitos de uso, coerência artística e custo gráfico precisam ser avaliados nos recursos escolhidos **para o próprio projeto atual**.

## Como encarar essas lembranças

São **alertas para raciocinar melhor**, não decisões ou instruções. Nenhum deles obriga a criar ferramenta, sistema de testes, painel, otimização, etapa de desenvolvimento ou funcionalidade adicional. Quando um risco semelhante surgir no IndexCities, a pergunta útil é: *esse problema pode acontecer com as regras e a implementação que estamos criando?* Se sim, avaliar ali mesmo a solução mais simples e apropriada.

**Não procurar o código ou os arquivos do experimento anterior por iniciativa própria.** Esta nota é intencionalmente autossuficiente: serve para transmitir as lições sem fornecer caminhos para a implementação antiga. O IndexCities deve continuar sendo projetado e construído do zero, conforme suas decisões atuais.
