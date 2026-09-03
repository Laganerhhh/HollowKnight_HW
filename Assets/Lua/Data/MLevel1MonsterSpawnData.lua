local MLevel1MonsterSpawnData = {
	sceneName = "Level1",
	legacyEnemies = {
		"Crawlid_0",
		"Crawlid_0 (1)",
		"Crawlid_0 (2)",
		"HuskDandy (2)",
		"GreatHusk",
	},
	spawns = {
		{
			spawnId = "level1_crawlid_01",
			monsterId = "Crawlid",
			position = { x = 17.97, y = -0.26999998, z = 0 },
			faceRight = true,
			patrol = { xMin = -6, xMax = 6 },
		},
		{
			spawnId = "level1_crawlid_02",
			monsterId = "Crawlid",
			position = { x = 5.28, y = -0.050000012, z = 0 },
			faceRight = true,
			patrol = { xMin = -6, xMax = 6 },
		},
		{
			spawnId = "level1_crawlid_03",
			monsterId = "Crawlid",
			position = { x = 46.72, y = -0.15999997, z = 0 },
			faceRight = true,
			patrol = { xMin = -6, xMax = 6 },
		},
		{
			spawnId = "level1_huskdandy_01",
			monsterId = "HuskDandy",
			position = { x = -27.45, y = -2.17, z = 0 },
			faceRight = false,
			patrol = { xMin = -2, xMax = 3 },
		},
		{
			spawnId = "level1_greathusk_01",
			monsterId = "GreatHusk",
			position = { x = 78.37, y = -5.18, z = 0 },
			faceRight = true,
			patrol = { xMin = -2, xMax = 2 },
		},
	},
}

return MLevel1MonsterSpawnData
