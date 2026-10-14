# EvoHud

[English](README.md) · **Português**

Overlay de telemetria para **Assetto Corsa EVO**: gráfico e barras de pedais (embreagem, freio, acelerador) com destaque quando ABS/TC atuam, níveis de ABS/TC/TCC, marcha, velocidade, luzes de troca, volante girando e linha de combustível (atual, consumo por volta e voltas restantes).

Lê a memória compartilhada do jogo (`Local\acevo_pmf_physics` / `Local\acevo_pmf_graphics`) diretamente. Não precisa de SimHub nem de instalar nada: compila com o .NET Framework que já vem no Windows.

> [!IMPORTANT]
> **O overlay só aparece quando o carro está na pista.** Ele fica escondido nos menus, no replay, na pausa e enquanto o carro está parado no box ou na garagem (tela de setup). Para ele aparecer: **engate a primeira marcha e acelere**, para o carro começar a andar.
> Se você não vê nada depois de abrir o jogo, isso é esperado: não é defeito. Comece a pilotar e ele aparece. Ele some de novo quando você para no box, pausa ou sai da sessão.

## Baixar

Baixe o `EvoHud-v1.1-windows.zip` na página de [Releases](../../releases), extraia e rode o `EvoHud.exe`. Mantenha a pasta `assets` ao lado do `.exe`.

## Usar

1. Rode `EvoHud.exe` (a pasta `assets` precisa estar ao lado dele).
2. O jogo deve estar em **janela** ou **tela cheia sem bordas**.
3. O HUD aparece só enquanto você está pilotando. Some em menus, replay, pausa, parado no box (setup) e quando o jogo não está em primeiro plano.
4. Ícone "E" na bandeja do sistema (perto do relógio), clique direito:
   - **Unlock / Lock position**: destrava para arrastar (também `Ctrl+Shift+L` ou duplo clique no ícone).
   - **Configurações...**, **Tamanho**, **Restaurar posição**, **Modo demonstração**, **Esconder quando não está pilotando**, **Sair**.

**Sem pedal de embreagem?** Desmarque **Embreagem** em **Configurações...** (ou use `ShowClutch=0`). Com embreagem automática o jogo mexe no valor da embreagem sozinho, e a barra azul parece travada. A barra e a linha dela no gráfico somem e as outras duas barras se centralizam.

## Escolher o que ver

Clique com o botão direito no ícone da bandeja e escolha **Configurações...** (ou rode `EvoHud.exe --settings`). Marque só o que você quer no HUD. Ele se reorganiza sozinho: o que for escondido não ocupa espaço e o overlay encolhe para caber. Tudo vale na hora, fica salvo, e o HUD mostra dados de demonstração enquanto a janela está aberta, para você ver o efeito de cada opção.

| Grupo | Opções |
|---|---|
| Pedais | Gráfico dos pedais, barras dos pedais, embreagem (barra e linha do gráfico), níveis e atuação de ABS / TC / TCC |
| Painel | Marcha, velocidade, luzes de troca, volante |
| Combustível | Combustível no tanque, consumo por volta, voltas restantes |
| Aparência | Controles de **tamanho** (40% a 300%) e de **transparência**, e **Restaurar padrões** |

Sem o gráfico dos pedais o HUD fica compacto: as barras ficam ao lado de um cartão de combustível, com as caixinhas de ABS / TC / TCC numa linha logo abaixo das linhas de combustível.

A janela segue o idioma do seu Windows (português ou inglês).
## Redimensionar

Escolha o tamanho que quiser, de 40% a 300% do padrão. O tamanho é salvo e volta igual na próxima vez.

- **Ícone da bandeja, botão direito, Size**: 50%, 75%, 100%, 125%, 150%, 200%, 250%, ou Bigger (maior) / Smaller (menor).
- **Atalhos (funcionam a qualquer hora, inclusive jogando)**: `Ctrl+Shift++` aumenta, `Ctrl+Shift+-` diminui (teclas do teclado principal ou do numérico).
- **Com o mouse**: destrave o HUD (`Ctrl+Shift+L`), depois **gire a rodinha do mouse** sobre ele ou **arraste o canto amarelo no canto inferior direito**. Trave de novo quando terminar.

Também dá para definir à mão com `Scale` no `settings.ini` (por exemplo `Scale=1.5`).
## Configurar

`settings.ini` é criado ao lado do `.exe` (edite com o app fechado):

| Chave | Padrão | O que faz |
|---|---|---|
| `Scale` | `1.0` | Tamanho do HUD, de 0.4 a 3.0 (salvo quando você redimensiona) |
| `Opacity` | `0.94` | Transparência |
| `Wheel` | `assets\wheel.png` | Imagem do volante (PNG quadrado, fundo transparente, centralizado) |
| `InvertSteer` | `0` | Inverte o giro do volante |
| `ColorClutch` / `ColorBrake` / `ColorThrottle` | azul / vermelho / verde | Cores dos pedais |
| `ColorAbs` / `ColorTc` | laranja / amarelo | Cor quando ABS/TC atuam |
| `ColorShiftA` / `ColorShiftB` | vermelho / apagado | Pisca de troca de marcha |
| `ShowGraph`, `ShowBars`, `ShowClutch`, `ShowAids`, `ShowGear`, `ShowSpeed`, `ShowShiftLights`, `ShowWheel`, `ShowFuel`, `ShowFuelPerLap`, `ShowLapsLeft` | `1` | Mostra ou esconde cada parte (mais fácil pela janela de Configurações) |
| `AidColors` | `1` | Recolore os pedais enquanto ABS / TC atuam |
| `AutoHide`, `HideInPitBox`, `HideWhenGameNotFocused` | `1` | Regras de esconder |

## Trocar o volante

Coloque um PNG quadrado com fundo transparente em `assets\wheel.png`. Para uma foto com fundo branco:

```
powershell -ExecutionPolicy Bypass -File tools\cutout.ps1 C:\caminho\volante.jpg assets\wheel.png
```

## Compilar

```
powershell -ExecutionPolicy Bypass -File build.ps1
```

Gera `dist\EvoHud.exe` (+ `dist\assets`).

## Diagnóstico

`EvoHud.exe --probe` grava `probe.log` por 40 s com os valores brutos lidos do jogo. `EvoHud.exe --demo` abre com dados de demonstração.

