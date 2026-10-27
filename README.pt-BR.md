# SimracingBR Overlay

[English](README.md) · **Português**

Um conjunto de overlays de telemetria para simuladores de corrida, desenhados sobre o jogo (em janela ou tela cheia sem bordas). Um app, um ícone na bandeja, e você escolhe quais overlays mostrar:

| Overlay | O que mostra | Padrão |
|---|---|---|
| **Pedais** | Gráfico e barras dos pedais (embreagem, freio, acelerador) que mudam de cor quando ABS/TC atuam, níveis de ABS / TC / TCC, balança de freio, marcha, velocidade, luzes de troca, um volante que gira e a linha de combustível | ligado |
| **Setores** (BETA) | Volta atual / melhor / última, a melhor volta do líder e S1 / S2 / S3 divididos em subsetores pintados de roxo / verde / cinza, como o app CMRT do Assetto Corsa 1 | ligado |
| **Combustível** (BETA) | Combustível no tanque, consumo por volta, voltas restantes e o **combustível sugerido para a corrida inteira** | desligado |
| **Eletrônica** | Chips de ABS / TC / TCC / ESC, balança de freio, mapa do motor, diferencial, bateria do híbrido, energia virtual, DRS | desligado |
| ~~Pneus~~ | Assistente de pressão dos pneus: desligado por enquanto (veja [abaixo](#assistente-de-pressão-dos-pneus-desligado)) | – |

| Jogo | Fonte dos dados | Status |
|---|---|---|
| **Assetto Corsa EVO** | `Local\acevo_pmf_*` | Pronto, testado no jogo |
| **Assetto Corsa Competizione** | `Local\acpmf_*` | Implementado, falta testar no jogo |
| **Automobilista 2** | `$pcars2$` (formato Project CARS 2) | Implementado, falta testar no jogo |
| **Le Mans Ultimate** | `LMU_Data` (memória compartilhada nativa do LMU) | Implementado, falta testar no jogo |

O app descobre sozinho qual jogo está aberto. Não precisa de SimHub nem de instalar nada: compila com o .NET Framework que já vem no Windows.

> [!IMPORTANT]
> **Os overlays de pista só aparecem quando o carro está na pista.** Eles ficam escondidos nos menus, no replay, na pausa e com o carro parado no box ou na garagem. Se você abrir o jogo e não aparecer nada, é o esperado: comece a pilotar. O overlay de combustível também pode aparecer na tela de setup do jogo (veja [Quando mostrar](#overlay-de-combustível-beta)).

## Baixar

Uma versão pronta para rodar no Windows (não precisa instalar nada: só do .NET Framework que já vem no Windows). Baixe o zip de uma release, extraia e rode `SimracingBR.exe`, mantendo a pasta `assets` ao lado dele.

- [Releases do SimracingBR-overlay](https://github.com/eliezzer/SimracingBR-overlay/releases) (v0.2.0)
- [Releases do assetto-corsa-evo-hud](https://github.com/eliezzer/assetto-corsa-evo-hud/releases) (v2.1.0, a mais nova: correções do combustível online, transparência do fundo dos pedais, cor da faixa)

O SimracingBR-overlay v0.2.0 é o mesmo programa do assetto-corsa-evo-hud v2.0.0; a v2.1.0 por enquanto só existe aqui. A versão que o programa mostra na dica do ícone da bandeja é a dele (0.3.0 na v2.1.0). O programa não é assinado, então o Windows pode mostrar um aviso do SmartScreen na primeira vez: escolha *Mais informações* → *Executar assim mesmo*. Você também pode compilar você mesmo, veja [Compilar](#compilar).

## Configuração de cada jogo

- **AC EVO, ACC e LMU**: nada a fazer, a memória compartilhada já vem ligada.
- **Automobilista 2**: em *Options → System*, mude **Shared Memory** para **Project CARS 2**.
- Jogue em **janela** ou **tela cheia sem bordas** (o overlay não aparece sobre tela cheia exclusiva).

## Usar

1. Rode `SimracingBR.exe` (a pasta `assets` precisa estar ao lado dele).
2. Ícone "S" verde e amarelo na bandeja do sistema (perto do relógio). Clique com o botão direito:
   - **Configurações...**: um menu lateral com uma entrada por overlay e um botão liga/desliga à direita dele. Clique no nome para ver as configurações daquele overlay ao lado; o de pedais já vem selecionado. As mudanças valem na hora e os overlays mostram dados de demonstração enquanto a janela estiver aberta.
   - **Destravar / Travar posição**: destrava para arrastar os overlays (também `Ctrl+Shift+L` ou duplo clique no ícone). Também dá para passar o mouse sobre um overlay e clicar no botão **DESTRAVAR** / **TRAVAR** no canto superior direito dele.
   - **Overlays**, **Tamanho**, **Restaurar posições**, **Modo demonstração**, **Esconder quando não está pilotando**, **Sair**.
3. Redimensione com `Ctrl+Shift++` / `Ctrl+Shift+-` (vale para o overlay sob o mouse, ou o último que você usou), ou destrave e use a rodinha do mouse ou arraste o canto amarelo.

Cada overlay tem a sua posição, tamanho e transparência. BETA quer dizer que funciona, mas ainda está sendo ajustado.

## Overlay de pedais

Gráfico e barras dos pedais, chips de ABS / TC / TCC (acendem enquanto o auxílio atua), um chip **BB** com a balança de freio, marcha, velocidade, luzes de troca, volante e a linha de combustível. Em Configurações escolha o que aparece, o tamanho, a transparência geral e a do fundo (o fundo pode ficar quase invisível com os pedais sólidos). O chip **TCC** só aparece nos jogos e carros que têm esse ajuste, e o **BB** só quando o jogo publica a balança de freio.

## Overlay de setores (BETA)

Volta atual / melhor / última, a melhor volta do líder (só LMU e AMS2) e S1 / S2 / S3 com 8 subsetores cada um (de 1 a 12 em Configurações).

Os jogos só publicam os 3 setores principais (alguns nem isso), então o overlay cronometra cada trecho sozinho, com a posição na volta e o relógio dele; todos os jogos publicam a posição. As cores funcionam como no app CMRT do Assetto Corsa 1: cada trecho é pintado assim que você o percorre, e tudo volta a escuro depois da linha de chegada:

- **roxo**: o seu tempo é o melhor da sessão naquele trecho;
- **verde**: o seu melhor naquele trecho, mas outro piloto foi mais rápido;
- **cinza**: mais lento que o seu melhor naquele trecho;
- **escuro**: ainda não passou por ali nesta volta.

O "melhor da sessão" é comparado com **todos os pilotos** só onde o jogo publica os tempos deles (setores principais no AMS2, setor 1 no LMU). Onde não publica (subsetores, AC EVO, ACC), roxo quer dizer o seu próprio melhor e só aparece enquanto você está sozinho na sessão; com outros pilotos, o seu melhor fica verde. Há uma opção para pintar o seu melhor de roxo mesmo assim.

O tempo da volta atual fica vermelho quando a volta é inválida e roxo enquanto você está à frente da sua melhor volta; o MELHOR fica roxo quando é a volta mais rápida da sessão (LMU / AMS2). Os limites dos setores principais são aprendidos pelas trocas de setor do jogo (ACC, LMU, AMS2) e lembrados por pista; o AC EVO não publica o setor, então usa terços da volta.

## Overlay de combustível (BETA)

Combustível no tanque (com barra de nível), consumo por volta, voltas restantes no tanque e **COMBUSTÍVEL SUGERIDO**: o combustível para a **corrida inteira** (nunca "quanto completar").

```
combustível sugerido = (voltas da corrida + margem de segurança) * consumo por volta
voltas da corrida    = as voltas da corrida, ou: tempo da corrida / sua volta válida mais rápida + 0,5   (corrida por tempo)
```

- **Consumo**: vem do jogo quando ele tem, senão é medido em voltas limpas. Fica guardado por pista e carro.
- **Volta válida mais rápida**: a melhor volta válida vista naquela pista e carro em qualquer sessão (a volta do qualy conta), guardada entre uma execução e outra. Voltas inválidas e voltas pelo box não contam.
- **Margem de segurança**: 1 volta de combustível por padrão (de 0 a 3 em Configurações).
- **Duração da corrida**, nesta ordem: o que você definir em Configurações (voltas ou minutos) → a definição do evento do jogo (veja [Como o AC EVO é lido](#como-o-ac-evo-é-lido), vale para servidores online antes de a corrida começar) → o log de um evento solo → a própria sessão de corrida (o relógio no começo da corrida) → a última corrida que o overlay viu. Se nenhuma for conhecida, ele pede para você definir.
- **Quando mostrar**: na pista, na **tela de setup** do jogo, ou nos dois (os dois por padrão). Na tela de setup o overlay aparece onde você o posicionou e some quando você sai dela, então o número está lá enquanto você digita o setup. A tela de setup é detectada por jogo: AC EVO (carro parado com o motor desligado, descoberto com o `--probe`; o menu principal da garagem tem o motor em marcha lenta), ACC (`isSetupMenuVisible`), LMU (box de garagem). Os arquivos de setup do jogo nunca são tocados.

## Overlay de eletrônica

Chips de ABS / TC / TCC / ESC que acendem enquanto o auxílio atua, mais balança de freio, mapa do motor, diferencial (potência / freio motor), bateria do híbrido, energia virtual (LMU) e DRS. Só aparece o que o carro e o jogo publicam: um GT3 do AC EVO mostra ABS, TC, balança de freio e diferencial; um hipercarro no LMU acrescenta mapa do motor, bateria e energia.

## O que cada jogo fornece

| Dado | AC EVO | ACC | AMS2 | LMU |
|---|---|---|---|---|
| Pedais, marcha, RPM, velocidade | ✓ | ✓ | ✓ | ✓ |
| Giro do volante | ✓ (graus do jogo) | `AccSteerLock` | `Ams2SteerLock` | ✓ (faixa do volante do jogo) |
| Níveis de ABS / TC | ✓ | ✓ | ✓ | ✓ |
| Máximo dos níveis de ABS / TC | ✓ | – | – | ✓ |
| TCC, nível de ESC | ✓ | TCC | – | TCC quando o carro tem |
| ABS atuando / TC atuando | ✓ / ✓ | ✓ / ✓ | ✓ / – | ✓ / ✓ |
| Balança de freio | ✓ | ✓ | ✓ | ✓ |
| Mapa do motor | – | ✓ | – | ✓ |
| Diferencial | ✓ | – | – | – |
| Bateria / energia virtual | bateria (se o carro tem KERS) | – | – | ✓ / ✓ |
| DRS | disponível | – | ✓ | ✓ |
| Consumo por volta | do jogo | do jogo | medido | medido |
| Capacidade do tanque | ✓ | ✓ | ✓ | ✓ |
| Posição na volta | ✓ | ✓ | ✓ | ✓ |
| Índice do setor | – | ✓ | ✓ | ✓ |
| Melhor / última volta, volta válida | ✓ | ✓ | ✓ | ✓ |
| Melhor volta do líder / da sessão | – | – | ✓ | ✓ |
| Voltas da corrida / tempo restante | – (veja abaixo) | ✓ | ✓ | ✓ |
| Pilotos na sessão | ✓ | ✓ | ✓ | ✓ |
| Tela de setup | inferida | ✓ | – | garagem |

O ACC e o AMS2 só informam o volante de -1 a 1. O overlay converte para graus usando o giro total do volante, que é `540` por padrão. Ajuste no `settings.ini` (`AccSteerLock=`, `Ams2SteerLock=`) para combinar com o carro.

Só o **AC EVO** foi conferido em sessões reais. Para os outros jogos as leituras foram escritas a partir das estruturas documentadas, e a saída do `--probe` é a forma de verificá-las.

### Como o AC EVO é lido

O AC EVO publica os dados do jogador na memória compartilhada, mas nada sobre os outros carros, nem a duração da corrida. Dois arquivos do próprio jogo são lidos (só leitura, nunca escritos) para preencher essa lacuna:

- `Saved Games\ACE\last_season.seasondefinition`: a definição do evento, que o jogo regrava quando você entra num servidor ou começa um evento (Protocol Buffers). Para cada sessão, as configurações ficam em `25 -> 2 -> 200`: o campo 3 é a duração em ms de uma sessão por tempo e o campo 5 o número de voltas de uma corrida por voltas. Visto: um qualy de 10 minutos tem `3 = 600000`, uma corrida de 10 voltas tem `5 = 10`. **Corridas por tempo ainda não foram vistas**, então esse caso é um palpite.
- `Saved Games\ACE\Logs\log-*.txt`: numa corrida solo (Race Weekend), a linha "Game Started!" traz a duração de cada sessão.

## Configurar

`settings.ini` é criado ao lado do `.exe` (edite com o app fechado; a maior parte é mais fácil em **Configurações**). Cada overlay guarda `<Prefixo>X`, `<Prefixo>Y`, `<Prefixo>Scale`, `<Prefixo>Opacity` e `<Prefixo>Enabled`, com os prefixos `Sectors`, `Fuel`, `Elec` (o overlay de pedais usa `X`, `Y`, `Scale`, `Opacity` e `PedalsEnabled`, sem prefixo).

| Chave | Padrão | O que faz |
|---|---|---|
| `Scale` | `1.0` | Tamanho do overlay de pedais, de 0.4 a 3.0 |
| `Opacity` | `0.94` | Transparência do overlay de pedais inteiro (pedais, marcha, volante e fundo), 0.2 a 1.0 |
| `BgOpacity` | `1.0` | Só o fundo do painel do overlay de pedais, 0.0 a 1.0, multiplicado pelo `Opacity` (abaixe para manter os pedais sólidos com o fundo transparente) |
| `Wheel` | `assets\wheel.png` | Imagem do volante (PNG quadrado, fundo transparente) |
| `InvertSteer` | `0` | Inverte o giro do volante |
| `AccSteerLock` / `Ams2SteerLock` / `LmuSteerLock` | `540` | Giro total do volante em graus (o LMU usa o valor do jogo quando existe) |
| `ColorClutch` / `ColorBrake` / `ColorThrottle` | azul / vermelho / verde | Cores dos pedais |
| `ColorAbs` / `ColorTc` | laranja / amarelo | Cor quando ABS/TC atuam |
| `ColorShiftA` / `ColorShiftB` | vermelho / apagado | Pisca de troca de marcha |
| `ColorStripe` | `#E10600` (vermelho) | Faixa à esquerda de todos os overlays (também em **Configurações** > Aparência) |
| `ShowGraph`, `ShowBars`, `ShowClutch`, `ShowAids`, `ShowBias`, `ShowGear`, `ShowSpeed`, `ShowShiftLights`, `ShowWheel`, `ShowFuel`, `ShowFuelPerLap`, `ShowLapsLeft` | `1` | Mostra ou esconde cada parte do overlay de pedais |
| `AidColors` | `1` | Recolore os pedais enquanto ABS / TC atuam |
| `AutoHide`, `HideInPitBox`, `HideWhenGameNotFocused` | `1` | Regras de esconder dos overlays de pista |
| `SectorsSubs` | `8` | Subsetores por setor, de 1 a 12 |
| `SectorsShowTimes`, `SectorsShowLeader`, `SectorsShowSectorTimes` | `1` | Partes do overlay de setores |
| `SectorsPurpleBest` | `0` | Pinta o seu melhor de roxo mesmo quando o jogo não mostra os outros pilotos |
| `ColorSecPurple` / `ColorSecGreen` / `ColorSecSlower` | roxo / verde / cinza | Cores dos setores |
| `FuelVis` | `2` | Onde o combustível aparece: `0` na pista, `1` tela de setup, `2` os dois |
| `FuelShowFuel`, `FuelShowPerLap`, `FuelShowLapsLeft`, `FuelShowSuggested` | `1` | Partes do overlay de combustível |
| `FuelMargin` | `1.0` | Margem de segurança em voltas de combustível |
| `FuelRaceLaps` / `FuelRaceMinutes` | `0` | Duração da corrida que você definiu (`0` = automática) |
| `ElecShowAbs`, `ElecShowTc`, `ElecShowTcc`, `ElecShowEsc`, `ElecShowBias`, `ElecShowMap`, `ElecShowDiff`, `ElecShowBattery`, `ElecShowEnergy`, `ElecShowDrs` | `1` | Partes do overlay de eletrônica |

O arquivo também guarda o que o app aprende: `FuelBest_*` e `FuelPer_*` (volta válida mais rápida e consumo por pista e carro), `FuelLastRace*` (a última corrida vista) e `SecBounds_*` (limites dos setores por pista).

## Compilar

```
powershell -ExecutionPolicy Bypass -File build.ps1
```

Gera `dist\SimracingBR.exe` (+ `dist\assets`).

## Diagnóstico

- `SimracingBR.exe --demo` abre com dados de demonstração.
- `SimracingBR.exe --probe` grava `probe.log` por 5 minutos com os valores brutos de todos os jogos suportados, o estado do cronometrista de voltas (linha `tracker:`) e os valores de combustível / eletrônica (linha `extras:`). Rode com o carro na pista e mande o arquivo se algo parecer errado.
- `SimracingBR.exe --dump` (AC EVO) grava uma pasta `dump` por 5 minutos com cópias das páginas da memória compartilhada, para procurar dados que o jogo possa publicar.

## Estrutura

```
src/Program.cs                 ponto de entrada (--demo, --probe, --dump)
src/Core/Model.cs              modelo de telemetria (Tele), base de memória compartilhada, settings
src/Core/SectorTracker.cs      cronometragem de setores / subsetores e cores
src/Core/FuelModel.cs          consumo, volta válida mais rápida, duração da corrida, combustível sugerido
src/Core/TyreModel.cs          assistente de pressão dos pneus (desligado)
src/Sources/AcEvoSource.cs     Assetto Corsa EVO
src/Sources/EvoSeason.cs       AC EVO: definição do evento (duração da corrida)
src/Sources/EvoLog.cs          AC EVO: log de um evento solo (duração da corrida)
src/Sources/AccSource.cs       Assetto Corsa Competizione
src/Sources/Ams2Source.cs      Automobilista 2
src/Sources/LmuSource.cs       Le Mans Ultimate
src/Sources/GameSources.cs     detecção automática do jogo + dados de demonstração
src/UI/OverlayApp.cs           bandeja, atalhos, janela de Configurações, qual overlay aparece quando
src/UI/OverlayWindow.cs        base de toda janela de overlay (travar, arrastar, redimensionar, clicar através)
src/UI/Chrome.cs               visual comum dos overlays
src/UI/HudView.cs, PedalsWindow.cs      overlay de pedais
src/UI/SectorsView.cs, SectorsWindow.cs overlay de setores
src/UI/FuelView.cs, FuelWindow.cs       overlay de combustível
src/UI/ElecView.cs, ElecWindow.cs       overlay de eletrônica
src/UI/TyreView.cs, TyreWindow.cs       overlay de pneus (desligado)
```

Para suportar outro jogo: crie uma classe `ITelemetrySource` (normalmente herdando de `ShmSource`) que preencha o `Tele`, e adicione em `GameSources`. Para criar outro overlay: derive uma janela de `OverlayWindow`, dê a ela uma view que implemente `IOverlayView` e adicione no `OverlayApp`.

## Assistente de pressão dos pneus (desligado)

Indisponível no momento: está comentado em `OverlayApp.cs` até funcionar bem. O código (`TyreModel`, `TyreView`, `TyreWindow`) foi mantido. Ele mostra a pressão a quente dos quatro pneus e a temperatura e, depois de duas voltas válidas, a **pressão a frio para colocar no setup** para que a pressão a quente fique no alvo (27,0 psi por padrão): `sugestão = alvo * frio / quente`, onde frio é a pressão no começo da sessão e quente a média da última volta válida. Exemplo: setup 25,5, quente 27,9 → 27,0 × 25,5 / 27,9 = 24,7.
