// Ajusta a validação do jQuery Validate para o padrão brasileiro:
// aceita vírgula como separador decimal (ex.: 89,90) e ponto como separador de milhar (ex.: 1.250,00).
(function ($) {
    if (!$ || !$.validator) {
        return;
    }

    function paraNumero(valor) {
        return parseFloat(String(valor).replace(/\./g, "").replace(",", "."));
    }

    $.validator.methods.number = function (valor, elemento) {
        return this.optional(elemento) || /^-?(?:\d+|\d{1,3}(?:\.\d{3})+)?(?:,\d+)?$/.test(valor);
    };

    $.validator.methods.range = function (valor, elemento, parametros) {
        var numero = paraNumero(valor);
        return this.optional(elemento) || (numero >= parametros[0] && numero <= parametros[1]);
    };

    $.validator.methods.min = function (valor, elemento, parametro) {
        return this.optional(elemento) || paraNumero(valor) >= parametro;
    };

    $.validator.methods.max = function (valor, elemento, parametro) {
        return this.optional(elemento) || paraNumero(valor) <= parametro;
    };
})(window.jQuery);
