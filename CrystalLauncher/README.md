# Crystal Launcher

Launcher moderno em WPF inspirado nos patchers de MMORPG clássicos. Ele carrega configurações a partir de `Config/launcher.json`, permite apontar para um client local (ex.: `C:/crystalserver/15.11 localhost`) e já está estruturado para sincronia via `raw.githubusercontent.com`.

## Recursos principais

- UI customizada com tema translúcido, cards de notícias e patch notes.
- Carregamento assíncrono das configurações com fallback local (`Config/launcher.json`) e opção de override remoto (`rawConfigUrl`).
- Botões para jogar, baixar o client a partir de um pacote hospedado e verificar integridade (`assets.json`).
- Atualização automática do status do servidor quando configurado um endpoint JSON.
- Suporte a feeds de notícias/patch notes locais ou remotos (JSON).

## Estrutura de configuração

```
Config/
 ├─ launcher.json             # Configuração ativa (copiada do template na primeira execução)
 ├─ launcher.template.json    # Exemplo com todos os campos suportados
```

Campos importantes de `launcher.json`:

- `clientDirectory`: diretório do client (aceita relativo ao launcher, ex.: `Clients/Multiverse`).
- `gameExecutable`: executável relativo ao diretório do client (`bin/multiverse_client.exe`).
- `assetsManifest`: Manifesto de assets usado na verificação (`assets.json`).
- `rawConfigUrl`: URL para sobrescrever a configuração via `raw.githubusercontent.com`.
- `news` / `patchNotes`: coleções exibidas na UI se não houver feeds remotos.
- `newsFeed` / `patchNotesFeed`: URLs de JSON para carregar dados dinâmicos.
- `statusEndpoint`: URL JSON com o estado do servidor (`state`, `status`, `online` ou `maintenance`).
- `downloadPackageUrl` / `downloadPackageFileName`: endereço e nome do arquivo que será baixado pelo botão **Download**.

O `launcher.json` distribuído aqui utiliza apenas caminhos genéricos e pode ser versionado/publicado sem expor diretórios locais sensíveis.

## Publicando no GitHub (`Laslest/MultiverseLauncher`)

1. Crie o repositório vazio em <https://github.com/Laslest/MultiverseLauncher>.
2. No PowerShell, inicialize o repositório local dentro de `c:\crystalserver\launcher\CrystalLauncher`:
   ```powershell
   git init
   git add .
   git commit -m "Launcher WPF inicial"
   git branch -M main
   git remote add origin https://github.com/Laslest/MultiverseLauncher.git
   git push -u origin main
   ```
3. Após o push, copie o conteúdo de `Config/launcher.json` para o repositório remoto e mantenha-o versionado. O link bruto ficará disponível em:
   `https://raw.githubusercontent.com/Laslest/MultiverseLauncher/main/Config/launcher.json`
4. Opcional: adicione também um `status.json`, `news.json` e `patchnotes.json` no repositório e atualize os campos `statusEndpoint`, `newsFeed` e `patchNotesFeed` para esses arquivos.

## Atualizando o Raw config
Sempre que editar `Config/launcher.json` localmente:

1. Abra o arquivo pelo botão **Configurações** no launcher ou via editor.
2. Faça o commit e `git push` para o GitHub.
3. O launcher tentará baixar o arquivo em `rawConfigUrl` na próxima inicialização. Caso prefira atualizar em tempo real, clique em **Configurações** > edite > feche o launcher e abra novamente.

## Verificação do client

O botão **Verificar** lê o `assets.json` localizado no diretório do client e acusa arquivos ausentes. Ajuste `assetsManifest` caso utilize outro arquivo.

## Próximos passos sugeridos

- Substituir as notícias e patch notes estáticas por endpoints reais.
- Adicionar barra de download com atualização real (HTTP + checksum) usando os hashes do manifesto.
- Incluir assets (logo, background animado, sons) para personalizar com a identidade do servidor.
- Empacotar o projeto com `dotnet publish -c Release` e distribuir o conteúdo de `bin/Release/net9.0-windows`.
