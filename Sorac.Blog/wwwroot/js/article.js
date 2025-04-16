export default class extends BlazorJSComponents.Component {
    setParameters() {
        let codes = Array.from(document.querySelectorAll('pre code'));
        let langs = codes.flatMap(code => Array.from(code.classList).filter(it => it.startsWith('language-')).map(it => it.slice(9)));
        import('https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/es/highlight.min.js').then(module => {
            window.hljs = module.default;
            Promise.all(langs.map(async lang => {
                if (hljs.getLanguage(lang) === undefined) {
                    try {
                        await import(`https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/${lang}.min.js`);
                    } catch (e) {
                        console.warn(`Failed to load language ${lang}: ${e}`);
                    }
                }
            })).then(() => hljs.highlightAll());
        });
        import("https://cdn.jsdelivr.net/npm/katex@0.16.22/dist/contrib/auto-render.mjs").then(module => { module.default(document.getElementById('article')); });
    }
}
