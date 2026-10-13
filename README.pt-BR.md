# EvoHud

[English](README.md) · **Português**

Overlay de telemetria para **Assetto Corsa EVO**: gráfico e barras de pedais (embreagem, freio, acelerador) com destaque quando ABS/TC atuam, níveis de ABS/TC/TCC, marcha, velocidade, luzes de troca, volante girando e linha de combustível (atual, consumo por volta e voltas restantes).

Lê a memória compartilhada do jogo (`Local\acevo_pmf_physics` / `Local\acevo_pmf_graphics`) diretamente. Não precisa de SimHub nem de instalar nada: compila com o .NET Framework que já vem no Windows.

> [!IMPORTANT]
> **O overlay só aparece quando o carro está na pista.** Ele fica escondido nos menus, no replay, na pausa e enquanto o carro está parado no box ou na garagem (tela de setup). Para ele aparecer: **engate a primeira marcha e acelere**, para o carro começar a andar.
> Se você não vê nada depois de abrir o jogo, isso é esperado: não é defeito. Comece a pilotar e ele aparece. Ele some de novo quando você para no box, pausa ou sai da sessão.

## Baixar

Baixe o `EvoHud-v1.0-windows.zip` na página de [Releases](../../releases), extraia e rode o `EvoHud.exe`. Mantenha a pasta `assets` ao lado do `.exe`.

## Usar

1. Rode `EvoHud.exe` (a pasta `assets` precisa estar ao lado dele).
2. O jogo deve estar em **janela** ou **tela cheia sem bordas**.
3. O HUD aparece só enquanto você está pilotando. Some em menus, replay, pausa, parado no box (setup) e quando o jogo não está em primeiro plano.
4. Ícone "E" na bandeja do sistema (perto do relógio), clique direito:
   - **Unlock / Lock position**: destrava para arrastar (também `Ctrl+Shift+L` ou duplo clique no ícone).
   - **Reset position**, **Demo mode**, **Hide when not driving**, **Quit**.

## Configurar

`settings.ini` é criado ao lado do `.exe` (edite com o app fechado):

| Chave | Padrão | O que faz |
|---|---|---|
| `Scale` | `1.0` | Tamanho do HUD |
| `Opacity` | `0.94` | Transparência |
| `Wheel` | `assets\wheel.png` | Imagem do volante (PNG quadrado, fundo transparente, centralizado) |
| `InvertSteer` | `0` | Inverte o giro do volante |
| `ColorClutch` / `ColorBrake` / `ColorThrottle` | azul / vermelho / verde | Cores dos pedais |
| `ColorAbs` / `ColorTc` | laranja / amarelo | Cor quando ABS/TC atuam |
| `ColorShiftA` / `ColorShiftB` | vermelho / apagado | Pisca de troca de marcha |
| `ShowGraph`, `ShowBars`, `ShowClutch`, `ShowGear`, `ShowWheel`, `ShowFuel`, `AidColors` | `1` | Liga/desliga partes do HUD |
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

