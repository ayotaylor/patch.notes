const { defineConfig } = require("@vue/cli-service");
module.exports = defineConfig({
	transpileDependencies: true,
	devServer: {
		client: {
			webSocketURL: "auto://0.0.0.0:0/ws",
		},
	},
	configureWebpack: {
		resolve: {
			alias: {
				"@": require("path").resolve(__dirname, "src"),
			},
		},
	},
});
