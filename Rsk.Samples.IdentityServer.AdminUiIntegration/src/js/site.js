var ids4Site = {

	init: function() {
		// remove preload class from body
		document.body.classList.remove('preload');
		// tell css we have js
		var html = document.getElementsByTagName('html')[0];
		html.classList.remove('no-js');
		html.classList.add('js');
		// setup smooth scrolling links
		ids4Site.smoothScroll();
	},

	smoothScroll: function() {
		var links = document.querySelectorAll('a[href*="#"]:not(.js-no-smooth-scroll)'), click = function (e) {
			var link = this.href.split('#');
			if (link.length > 1 && link[1] != '') {
				var dest = document.getElementById(link[1]);
				if (dest != null) {
					dest.scrollIntoView({
						block: 'start',
						behavior: 'smooth'
					});
					e.preventDefault();
				}
			}
		}, i;
		for (i = 0; i < links.length; i++) {
			links[i].onclick = click;
		}
	}

};

window.addEventListener('load', function (e) {
	ids4Site.init();
});