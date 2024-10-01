package tooth

const metadataJSONSchema = `{
	"$schema": "https://json-schema.org/draft-07/schema#",
	"type": "object",
	"properties": {
		"format_version": {
			"type": "integer",
			"const": 2
		},
		"tooth": {
			"type": "string"
		},
		"version": {
			"type": "string"
		},
		"info": {
			"type": "object",
			"properties": {
				"name": {
					"type": "string"
				},
				"description": {
					"type": "string"
				},
				"author": {
					"type": "string"
				},
				"tags": {
					"type": "array",
					"items": {
						"type": "string",
						"pattern": "^[a-z0-9-]+(:[a-z0-9-]+)?$"
					}
				},
				"avatar_url": {
					"type": "string"
				}
			},
			"required": [
				"name",
				"description",
				"author",
				"tags"
			]
		},
		"asset_url": {
			"type": "string"
		},
		"commands": {
			"type": "object",
			"properties": {
				"pre_install": {
					"type": "array",
					"items": {
						"type": "string"
					}
				},
				"post_install": {
					"type": "array",
					"items": {
						"type": "string"
					}
				},
				"pre_uninstall": {
					"type": "array",
					"items": {
						"type": "string"
					}
				},
				"post_uninstall": {
					"type": "array",
					"items": {
						"type": "string"
					}
				}
			}
		},
		"dependencies": {
			"type": "object",
			"patternProperties": {
				"^.*$": {
					"type": "string"
				}
			}
		},
		"prerequisites": {
			"type": "object",
			"patternProperties": {
				"^.*$": {
					"type": "string"
				}
			}
		},
		"files": {
			"type": "object",
			"properties": {
				"place": {
					"type": "array",
					"items": {
						"type": "object",
						"properties": {
							"src": {
								"type": "string"
							},
							"dest": {
								"type": "string"
							}
						},
						"required": [
							"src",
							"dest"
						]
					}
				},
				"preserve": {
					"type": "array",
					"items": {
						"type": "string"
					}
				},
				"remove": {
					"type": "array",
					"items": {
						"type": "string"
					}
				}
			}
		},
		"platforms": {
			"type": "array",
			"items": {
				"type": "object",
				"properties": {
					"goarch": {
						"type": "string"
					},
					"goos": {
						"type": "string"
					},
					"asset_url": {
						"type": "string"
					},
					"commands": {
						"type": "object",
						"properties": {
							"pre_install": {
								"type": "array",
								"items": {
									"type": "string"
								}
							},
							"post_install": {
								"type": "array",
								"items": {
									"type": "string"
								}
							},
							"pre_uninstall": {
								"type": "array",
								"items": {
									"type": "string"
								}
							},
							"post_uninstall": {
								"type": "array",
								"items": {
									"type": "string"
								}
							}
						}
					},
					"dependencies": {
						"type": "object",
						"patternProperties": {
							"^.*$": {
								"type": "string"
							}
						}
					},
					"prerequisites": {
						"type": "object",
						"patternProperties": {
							"^.*$": {
								"type": "string"
							}
						}
					},
					"files": {
						"type": "object",
						"properties": {
							"place": {
								"type": "array",
								"items": {
									"type": "object",
									"properties": {
										"src": {
											"type": "string"
										},
										"dest": {
											"type": "string"
										}
									},
									"required": [
										"src",
										"dest"
									]
								}
							},
							"preserve": {
								"type": "array",
								"items": {
									"type": "string"
								}
							}
						}
					}
				},
				"required": [
					"goos"
				]
			}
		}
	},
	"required": [
		"format_version",
		"tooth",
		"version",
		"info"
	]
}`
