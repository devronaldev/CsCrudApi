# CsCrudApi - Conectando Saberes 📚🔗

<p>CsCrudApi é uma API desenvolvida para a plataforma Conectando Saberes, um ambiente dedicado à publicação e comunicação de projetos de iniciação científica. Esta API fornece serviços essenciais, como autenticação de usuários, publicação de conteúdo, gerenciamento de interações e muito mais.
</p> 

<h2>Objetivo do Projeto 🎯</h2>
<p>O objetivo da plataforma Conectando Saberes é proporcionar um ambiente acadêmico interativo, onde alunos e orientadores podem compartilhar conhecimentos científicos e se conectar com outros pesquisadores. A API suporta funcionalidades de CRUD (criação, leitura, atualização e exclusão) para diversos recursos, permitindo uma experiência completa para o gerenciamento de conteúdos científicos.
</p>

# Tecnologias Utilizadas 💻

<p>A CsCrudApi foi desenvolvida com as seguintes tecnologias:</p>
<ul>
  <li><b>ASP.NET Core 8:</b> Framework principal utilizado para o desenvolvimento da API, proporcionando segurança, robustez e alta performance.</li>
  <li><b>Entity Framework Core:</b> Para mapeamento objeto-relacional (ORM), facilitando o acesso e manipulação de dados no banco de dados.</li>
  <li><b>MySQL via Google Cloud SQL:</b> Banco de dados para armazenar informações de usuários, posts, comentários, e muito mais.</li>
  <li><b>Autenticação JWT:</b> Para assegurar que apenas usuários autenticados acessem os endpoints protegidos.</li>
  <li><b>Migrations:</b> Gerenciamento e versionamento das mudanças no banco de dados.</li>
  <li><b>Docker:</b> Para deploy e escalabilidade de forma eficiente (opcional para deploy em contêiner).</li>
  <li><b>Variáveis de ambiente:</b> Configurações sensíveis e informações de conexão armazenadas em variáveis de ambiente para maior segurança.</li>
</ul>

# Funcionalidades Principais 🚀

<p>A CsCrudApi oferece várias funcionalidades que possibilitam uma comunicação rica e interativa entre usuários, incluindo:</p>

<ul>
  <li>Autenticação e Autorização:</li>
  <ul>
    <li>Registro e login de usuários</li>
    <li>Recuperação e redefinição de senha</li>
    <li>Gerenciamento de tokens de autenticação com JWT</li>
  </ul>
  <li>Gerenciamento de Publicações:</li>
  <ul>
    <li>Criar, editar e deletar posts científicos</li>
    <li>Associação de posts a autores e áreas de pesquisa</li>
    <li>Sistema de curtidas e de salvamento de posts por outros usuários</li>
  </ul>
  <li>Comunicação e Interatividade:</li>
  <ul>
    <li>Seguimento de usuários</li>
    <li>Comentários em publicações e respostas encadeadas</li>
    <li>Sistema de notificações para atividades relevantes</li>
  </ul>
  <li>Pesquisa e Exploração de Conteúdo:</li>
  <ul>
    <li>Busca por posts usando hashtags e palavras-chave</li>
    <li>Filtragem de posts por área de pesquisa e autores</li>
    <li>Consulta a publicações por cidade e campus</li>
  </ul>
</ul>

# Estrutura do Banco de Dados 📊

O banco de dados foi modelado para suportar a interação social e o gerenciamento de conteúdos. Abaixo estão algumas das tabelas principais:
<ol>
  <li>User: Informações dos usuários, incluindo dados de perfil e configurações.</li>
  <li>Post: Armazena informações sobre cada publicação.</li>
  <li>Commentary: Tabela de comentários com suporte a encadeamento de respostas.</li>
  <li>Hashtags e TagsPost: Para adicionar e associar hashtags a posts.</li>
  <li>UserFollowsUser: Armazena o relacionamento de seguimento entre usuários.</li>
</ol>

# Instalação e Configuração 🛠️

<h2>Pré-requisitos</h2>
<ul>
  <li>.NET 8 SDK</li>
  <li>MySQL (ou uma instância configurada em algum provedor)</li>
  <li>Opcional: Docker para execução em contêineres</li>
</ul>
<h2>Passo a Passo</h2>
<ol>
  <li>Clone o repositório:</li>
  <ul>
    <li>git clone https://github.com/devronaldev/TP02SWII6.git</li>
    <li>cd CsCrudApi</li>
  </ul></p>
  <li>Configuração do Banco de Dados: Configure a string de conexão do banco de dados, incluindo servidor, porta, nome do banco, usuário e senha. É altamente recomendado usar variáveis de ambiente para essas informações sensíveis ou configurar diretamente no arquivo appsettings.json, conforme sua necessidade.</li>
  <li>Executar Migrations: Execute as migrations para criar as tabelas no banco de dados:</li>
  <ul>
    <li>dotnet ef database update</li>
  </ul>
  <li>Executar o Projeto:</li>
  <ul>
    <li>dotnet run</li>
  </ul>
</ol>

# Variáveis de Ambiente

<p>Para maior segurança, defina informações sensíveis, como a string de conexão, em variáveis de ambiente do sistema. Isso protege dados confidenciais como nome de usuário e senha do banco de dados.
</p>
