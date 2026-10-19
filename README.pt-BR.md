# SimracingBR Overlay

[English](README.md) · **Português**

Overlay de telemetria para simuladores de corrida. Mostra o gráfico e as barras dos pedais (embreagem, freio e acelerador), que mudam de cor quando ABS ou TC atuam, os níveis de ABS/TC/TCC, marcha, velocidade, luzes de troca, um volante que gira e o combustível (atual, consumo por volta e voltas restantes).

| Jogo | Fonte dos dados | Status |
|---|---|---|
| **Assetto Corsa EVO** | `Local\acevo_pmf_*` | Pronto (mesmo código do [EvoHud](https://github.com/eliezzer/assetto-corsa-evo-hud)) |
| **Assetto Corsa Competizione** | `Local\acpmf_*` | Implementado, falta testar no jogo |
| **Automobilista 2** | `$pcars2$` (formato Project CARS 2) | Implementado, falta testar no jogo |
| **Le Mans Ultimate** | `LMU_Data` (memória compartilhada nativa do LMU) | Implementado, falta testar no jogo |

O app descobre sozinho qual jogo está aberto. Não precisa de SimHub nem de instalar nada: compila com o .NET Framework que já vem no Windows.

> [!IMPORTANT]
> **O overlay só aparece quando o carro está na pista.** Ele fica escondido nos menus, no replay, na pausa e com o carro parado no box ou na garagem. Se você abrir o jogo e não aparecer nada, é o esperado: comece a pilotar.

## Configuração de cada jogo

- **AC EVO, ACC e LMU**: nada a fazer, a memória compartilhada já vem ligada.
- **Automobilista 2**: em *Options → System*, mude **Shared Memory** para **Project CARS 2**.
- Jogue em **janela** ou **tela cheia sem bordas** (o overlay não aparece sobre tela cheia exclusiva).

## Usar

1. Rode `SimracingBR.exe` (a pasta `assets` precisa estar ao lado dele).
2. Ícone "S" verde e amarelo na bandeja do sistema (perto do relógio). Clique com o botão direito:
   - **Configurações...**: escolha o que aparece no HUD, tamanho e transparência.
   - **Destravar / Travar posição**: destrava para arrastar (também `Ctrl+Shift+L` ou duplo clique no ícone). Também dá para passar o mouse sobre o HUD e clicar no botão **DESTRAVAR** / **TRAVAR** que aparece no canto superior direito.
   - **Tamanho**, **Restaurar posição**, **Modo demonstração**, **Esconder quando não está pilotando**, **Sair**.
3. Redimensione com `Ctrl+Shift++` / `Ctrl+Shift+-`, ou destrave e use a rodinha do mouse ou arraste o canto amarelo.

O chip **TCC** (corte do TC) só aparece nos jogos e carros que têm esse ajuste (AC EVO, ACC e alguns carros do LMU).

## O que cada jogo fornece

| Dado | AC EVO | ACC | AMS2 | LMU |
|---|---|---|---|---|
| Pedais, marcha, RPM, velocidade | ✓ | ✓ | ✓ | ✓ |
| Giro do volante | ✓ (graus do jogo) | `AccSteerLock` | `Ams2SteerLock` | ✓ (faixa do volante do jogo) |
| Níveis de ABS / TC | ✓ | ✓ | ✓ | ✓ |
| Nível de TCC | ✓ | ✓ | – | quando o carro tem |
| ABS atuando | ✓ | ✓ | ✓ | ✓ |
| TC atuando | ✓ | ✓ | – (o jogo não publica) | ✓ |
| Consumo por volta | do jogo | do jogo | medido pelo HUD | medido pelo HUD |

O ACC e o AMS2 só informam o volante de -1 a 1. O HUD converte para graus usando o giro total do volante, que é `540` por padrão. Ajuste no `settings.ini` (`AccSteerLock=`, `Ams2SteerLock=`) para combinar com o carro.

## Configurar

`settings.ini` é criado ao lado do `.exe` (edite com o app fechado):

| Chave | Padrão | O que faz |
|---|---|---|
| `Scale` | `1.0` | Tamanho do HUD, de 0.4 a 3.0 |
| `Opacity` | `0.94` | Transparência |
| `Wheel` | `assets\wheel.png` | Imagem do volante (PNG quadrado, fundo transparente) |
| `InvertSteer` | `0` | Inverte o giro do volante |
| `AccSteerLock` / `Ams2SteerLock` / `LmuSteerLock` | `540` | Giro total do volante em graus (o LMU usa o valor do jogo quando existe) |
| `ColorClutch` / `ColorBrake` / `ColorThrottle` | azul / vermelho / verde | Cores dos pedais |
| `ColorAbs` / `ColorTc` | laranja / amarelo | Cor quando ABS/TC atuam |
| `ColorShiftA` / `ColorShiftB` | vermelho / apagado | Pisca de troca de marcha |
| `ShowGraph`, `ShowBars`, `ShowClutch`, `ShowAids`, `ShowGear`, `ShowSpeed`, `ShowShiftLights`, `ShowWheel`, `ShowFuel`, `ShowFuelPerLap`, `ShowLapsLeft` | `1` | Mostra ou esconde cada parte |
| `AidColors` | `1` | Recolore os pedais enquanto ABS / TC atuam |
| `AutoHide`, `HideInPitBox`, `HideWhenGameNotFocused` | `1` | Regras de esconder |

## Compilar

```
powershell -ExecutionPolicy Bypass -File build.ps1
```

Gera `dist\SimracingBR.exe` (+ `dist\assets`).

## Diagnóstico

`SimracingBR.exe --probe` grava `probe.log` por 40 s com os valores brutos de todos os jogos suportados. Rode com o carro na pista e mande o arquivo se algo parecer errado. `SimracingBR.exe --demo` abre com dados de demonstração.

## Estrutura

```
src/Core/Model.cs           modelo de telemetria comum, base de memória compartilhada, settings
src/Sources/AcEvoSource.cs  Assetto Corsa EVO
src/Sources/AccSource.cs    Assetto Corsa Competizione
src/Sources/Ams2Source.cs   Automobilista 2
src/Sources/LmuSource.cs    Le Mans Ultimate
src/Sources/GameSources.cs  detecção automática do jogo + dados de demonstração
src/UI/HudView.cs           desenho do HUD
src/UI/HudWindow.cs         janela, bandeja, atalhos, configurações, auto-esconder
```

Para suportar outro jogo: crie uma classe `ITelemetrySource` (normalmente herdando de `ShmSource`) que preencha o `Tele`, e adicione em `GameSources`.
