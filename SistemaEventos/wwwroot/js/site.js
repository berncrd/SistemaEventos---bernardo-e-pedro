// Encontro — scripts gerais
document.addEventListener("DOMContentLoaded", function () {
    // Pede confirmação antes de enviar formulários marcados com data-confirmar
    document.querySelectorAll("form[data-confirmar]").forEach(function (formulario) {
        formulario.addEventListener("submit", function (evento) {
            if (!window.confirm(formulario.dataset.confirmar)) {
                evento.preventDefault();
            }
        });
    });

    // Pré-visualização da imagem escolhida no cadastro de eventos
    document.querySelectorAll("input[type=file][data-preview]").forEach(function (campo) {
        campo.addEventListener("change", function () {
            var alvo = document.querySelector(campo.dataset.preview);
            if (alvo && campo.files && campo.files[0]) {
                alvo.src = URL.createObjectURL(campo.files[0]);
            }
        });
    });
});
