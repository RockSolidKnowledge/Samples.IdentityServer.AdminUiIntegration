module.exports = function({ addVariant }) {
	addVariant('first-child', ({ modifySelectors, separator }) => {
		modifySelectors(({ className }) => {
			return `.first-child${separator}${className}:first-child`
		})
	})
}