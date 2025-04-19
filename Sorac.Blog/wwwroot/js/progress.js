export default class extends BlazorJSComponents.Component {
    setParameters({ progress, progressBar }, refs) {
        this.progress = progress;
        this.progressBar = progressBar;

        let elems = Object.values(refs);

        this.current = 0;
        this.count = elems.filter(e => !e.complete).length;
        console.log(elems)
        console.log(this.count)

        if (this.count === 0) {
            this.progress.style.height = "0";
            return;
        }

        elems.forEach((elem) =>
            elem.onload = () => {
                this.current++;
                this.updateProgress();
            }
        );
    }

    updateProgress() {
        this.progressBar.style.width = `${(this.current / this.count) * 100}%`;

        if (this.current >= this.count) {
            setTimeout(() => {
                this.progress.style.height = "0";
            }, 500);
        }
    }
}
