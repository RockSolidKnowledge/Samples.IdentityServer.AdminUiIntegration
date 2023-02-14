module.exports = function({ addVariant }) {
	addVariant('last-child', ({ modifySelectors, separator }) => {
		modifySelectors(({ className }) => {
			return `.last-child${separator}${className}:last-child`
		})
	})
}