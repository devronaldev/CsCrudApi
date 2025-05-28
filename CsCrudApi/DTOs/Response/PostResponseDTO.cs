using CsCrudApi.Models.PostRelated;

namespace CsCrudApi.DTOs
{
    /// <summary>
    /// Representa um Objeto de Transferência de Dados (DTO) para a requisição ou resposta de um post,
    /// incluindo suas informações básicas e categorias associadas.
    /// </summary>
    /// <remarks>
    /// Este DTO é utilizado para encapsular dados de um post, permitindo a inclusão opcional
    /// de categorias, seja para envio de novas postagens (requisição) ou para exibição
    /// de posts existentes (resposta), como na listagem de posts de um usuário.
    /// </remarks>
    public class PostResponseDTO
    {
        /// <summary>
        /// A entidade principal do post.
        /// </summary>
        /// <remarks>
        /// Contém os dados fundamentais do post, como GUID, título, conteúdo, data de publicação, etc.
        /// </remarks>
        public Post Post { get; set; }

        /// <summary>
        /// Uma lista opcional de IDs de categorias associadas ao post.
        /// </summary>
        /// <remarks>
        /// Este campo pode ser usado tanto para indicar as categorias ao criar/atualizar um post,
        /// quanto para listar as categorias às quais um post existente pertence.
        /// Pode ser nulo se não houver categorias ou se não for relevante para a operação.
        /// </remarks>
        /// <example>[1, 5, 8]</example>
        public List<int>? Categories { get; set; }

        /// <summary>
        /// Inicializa uma nova instância vazia do <see cref="PostRequestDTO"/>.
        /// </summary>
        /// <remarks>
        /// Este construtor padrão é útil para desserialização automática em requisições
        /// ou para inicialização manual antes de preencher os dados.
        /// As propriedades <see cref="Post"/> e <see cref="Categories"/> são inicializadas para evitar referências nulas.
        /// </remarks>
        public PostResponseDTO()
        {
            Post = new Post(); // Assume que Post é uma entidade ou classe que pode ser instanciada
            Categories = new List<int>();
        }

        /// <summary>
        /// Inicializa uma nova instância do <see cref="PostRequestDTO"/> com um post e uma lista de categorias.
        /// </summary>
        /// <param name="post">A entidade Post a ser encapsulada no DTO.</param>
        /// <param name="categories">Uma lista de IDs de categorias associadas ao post.</param>
        public PostResponseDTO(Post post, List<int> categories)
        {
            Post = post;
            Categories = categories;
        }

        /// <summary>
        /// Inicializa uma nova instância do <see cref="PostRequestDTO"/> apenas com a entidade do post,
        /// sem categorias iniciais.
        /// </summary>
        /// <remarks>
        /// Este construtor é útil quando se está mapeando um post existente do banco de dados
        /// e as categorias serão carregadas separadamente ou não são necessárias imediatamente.
        /// </remarks>
        /// <param name="post">A entidade Post a ser encapsulada no DTO.</param>
        public PostResponseDTO(Post post)
        {
            Post = post;
            Categories = new List<int>();
        }
    }
}
