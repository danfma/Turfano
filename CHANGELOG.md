# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="1.0.0-rc.1"></a>
## [1.0.0-rc.1](https://www.github.com/danfma/Turfano/releases/tag/v1.0.0-rc.1) (2026-07-02)

### Features

* add another set of packages ([e8154fe](https://www.github.com/danfma/Turfano/commit/e8154fe8885d83ab9660d99f94c792f7961ef3df))
* add efficiency report and optimize Territory.Along.cs ([8ef4192](https://www.github.com/danfma/Turfano/commit/8ef4192856fd43c403a0ee980370cc5c5870fb62))
* Add polygon smooth ([1ffe0e6](https://www.github.com/danfma/Turfano/commit/1ffe0e6a91ab29699acc417376de0b3602d7f03a))
* Add support to angle, rhumb-bearing, and helpers (BearingToAzimuth). ([dd2e456](https://www.github.com/danfma/Turfano/commit/dd2e456a420833e5352c029b9828016000d1b3a9))
* improve compatibility with existing libraries and fix issues ([397b5e8](https://www.github.com/danfma/Turfano/commit/397b5e876ef09abee6881a8773fb02d2fc9edc83))
* leva 2 — remove a superficie legada; core zero-dependencias; 1.0.0-rc.1 ([71d0d7c](https://www.github.com/danfma/Turfano/commit/71d0d7c335617112f7efc36df75ad78f2bc33087))
* **core:** Port initial set of Turf.js utility modules ([f5dfaa2](https://www.github.com/danfma/Turfano/commit/f5dfaa2c743fc49e1d28a318e650997b54b9bc8c))
* **eval:** avaliação NTS×TurfJS — decisão op-a-op + benchmark (Fase 2) ([9ca5360](https://www.github.com/danfma/Turfano/commit/9ca5360205a39458b45f28d489df5ebf33730ae8))
* **geojson:** fundação de tipos GeoJSON + serialização source-gen (T003-T007, US1) ([c6610c7](https://www.github.com/danfma/Turfano/commit/c6610c78c1eb3a800c6d6e8d958341e93c882495))
* **geojson:** helpers estilo Turf + ponte interna NTS (US4, T016-T018) ([10d986a](https://www.github.com/danfma/Turfano/commit/10d986a496834868cc3cba7c634b68400771289b))
* **nts:** satélite Turfano.NetTopologySuite — NtsConvert empacotado + Buffer extension (US3, T013-T018) ([aca01f6](https://www.github.com/danfma/Turfano/commit/aca01f689a6c1286a5ca6f229c63e8c7c7f7a424))
* **parity:** estatistica espacial + random/clusters/agregacao (Onda G US3+US4, T017-T027) ([4c1f352](https://www.github.com/danfma/Turfano/commit/4c1f3526df7941f31eab1f55941650ebdfeea427))
* **parity:** fecha US3 — distâncias/superfície na fachada Geo (Onda A completa) ([57184b4](https://www.github.com/danfma/Turfano/commit/57184b4e884805b40b454bc430783ee56115e8f5))
* **parity:** grades — pointGrid/squareGrid(+rectangle)/hexGrid/triangleGrid (Onda F US1, T001-T007) ([1a0c07a](https://www.github.com/danfma/Turfano/commit/1a0c07a2059b1874946c257dda2bd54e1b48e347))
* **parity:** hulls e tesselacao — convex/concave/tesselate/voronoi (Onda F US4, T017-T024) ([bd6f2b5](https://www.github.com/danfma/Turfano/commit/bd6f2b5825ce90df572ea3c124ae96cd3e9c7529))
* **parity:** isolines/isobands — marching squares fiel (Onda F US3, T013-T016) ([3c864eb](https://www.github.com/danfma/Turfano/commit/3c864ebb459b10b1f60b1bf688c7ef7e58cfa94d))
* **parity:** MVP da Onda A — measurement sobre os novos tipos (US1/US2 parcial) ([56c8db7](https://www.github.com/danfma/Turfano/commit/56c8db79da80faa3059c965bef454bb0a004152c))
* **parity:** operacoes de linha completas (Onda G US1, T007-T016) ([8b2b782](https://www.github.com/danfma/Turfano/commit/8b2b78258c8e517b6f3daad70ed4b7c3e76edf88))
* **parity:** planepoint/tin/interpolate (Onda F US2, T008-T012) ([d2257c7](https://www.github.com/danfma/Turfano/commit/d2257c701168f10543176753f6b5e946071b0663))
* **parity:** projection/mask/ellipse/sector/lineArc (Onda G US2, T001-T006) ([37c457d](https://www.github.com/danfma/Turfano/commit/37c457d934f46d8cac549aa9268d4de76a149638))
* **parity:** US1 — predicados de ponto/orientação na fachada Geo (Onda B) ([d5994d0](https://www.github.com/danfma/Turfano/commit/d5994d05be9c4e6411c2c9e1d55785b16258f36a))
* **parity:** US1 (mutação) + US2 (transformação geodésica) na fachada Geo ([4f0bca1](https://www.github.com/danfma/Turfano/commit/4f0bca1d055dd1944cb590b130bba526cec58e95))
* **parity:** US1 conversões estruturais na fachada Geo (Onda D) ([be4f9b2](https://www.github.com/danfma/Turfano/commit/be4f9b21011a41fb1082eb693ed2333c2ee9afae))
* **parity:** US1 overlay via NtsBridge na fachada Geo (Onda E) ([6fc00cd](https://www.github.com/danfma/Turfano/commit/6fc00cd277fb1c4249403019471d0028b948d436))
* **parity:** US1 polygonize via NtsBridge — Onda D completa (19 funções) ([753900a](https://www.github.com/danfma/Turfano/commit/753900af384f53b21f1d34e0d88e57d4db003f70))
* **parity:** US2 — disjoint/intersects/contains/within/equal na fachada Geo ([eabdb75](https://www.github.com/danfma/Turfano/commit/eabdb758707ef02446630ff1f66bde98ce67f96b))
* **parity:** US2 (pontos derivados) + US4 (conversões) na fachada Geo ([39c05e5](https://www.github.com/danfma/Turfano/commit/39c05e5988cf7202d368308fd9d0d00b5df81296))
* **parity:** US2 booleanTouches na fachada Geo — Onda B completa (14 predicados) ([acdea16](https://www.github.com/danfma/Turfano/commit/acdea16b6d215dd4eb8da8b8ea0fe9485ef31cdc))
* **parity:** US2 buffer (AEQD + NTS) — Onda E completa (6 funções) ([ba62f15](https://www.github.com/danfma/Turfano/commit/ba62f15ebd0d34456a6edd77aacbd7558dcad070))
* **parity:** US2 joins + utilitários de linha na fachada Geo (Onda D) ([65f1c2d](https://www.github.com/danfma/Turfano/commit/65f1c2de6ddefdeab094d466e64522dbef7b9cd0))
* **parity:** US2 overlap/crosses + US3 valid na fachada Geo ([b53caf1](https://www.github.com/danfma/Turfano/commit/b53caf1e4e615e39054c4216f4116793626005b4))
* **parity:** US3 bboxClip portado (Cohen-Sutherland) na fachada Geo (Onda E) ([63fdc31](https://www.github.com/danfma/Turfano/commit/63fdc318fa4461bb109173b2e3df83a7b7dc7786))
* **parity:** US3 circle + simplify na fachada Geo ([fb41087](https://www.github.com/danfma/Turfano/commit/fb41087fb3e1e9f57153cdf76f52455f4d14fa22))
* **parity:** US3 lineOffset + bezierSpline — Onda C completa (14 funções) ([37e181f](https://www.github.com/danfma/Turfano/commit/37e181f03088edcf8bef1cd47c1e3ac7027865c9))
* **parity:** US3 meta-iteração pública na fachada Geo (Onda D) ([4c1ae6f](https://www.github.com/danfma/Turfano/commit/4c1ae6f2ac30b365088ce7c8d75baa71337e3f9b))
* **parity:** US3 polygonSmooth (Chaikin) na fachada Geo ([2cad10b](https://www.github.com/danfma/Turfano/commit/2cad10b4635bad4c0e0755c6f86dac2218a8e43a))
* **parity:** US3 rumo (RhumbBearing/RhumbDistance) na fachada Geo ([aacab96](https://www.github.com/danfma/Turfano/commit/aacab962d530eaa57f7009bb40b01cf3b16c6f29))
* **polyclip:** fundações do porte — ExactDecimal + SplayTreeSet (T001-T003) ([06dc5a5](https://www.github.com/danfma/Turfano/commit/06dc5a583d21a789441166a8cd96bf3e9f566958))
* **polyclip:** polygonize nativo — porte do @turf/polygonize (US2, T011-T012) ([19d3b9f](https://www.github.com/danfma/Turfano/commit/19d3b9fddb988aaea09e372df156819960892b1a))
* **polyclip:** porte completo do motor Martinez-Rueda — overlay nativo (T004-T009) ([1c3149d](https://www.github.com/danfma/Turfano/commit/1c3149d1a947779abf38b126a7e95cedd330f579))
* **units:** structs próprios Length/Angle/Area = TurfJS (US2, T011-T013) ([b6adf3a](https://www.github.com/danfma/Turfano/commit/b6adf3a5c5518b83478a195a162326d7ed75da3a))

### Bug Fixes

* corrige Angles.TwoPi (2π) e precedência de eixo Y em TransformScale ([a1052db](https://www.github.com/danfma/Turfano/commit/a1052db9cbe0f462257659809f6720ba9b9310f1))
* translate Portuguese comments to English ([963d683](https://www.github.com/danfma/Turfano/commit/963d683ea3aa36343ad14c1e837931fe7cb8a004))

### Breaking Changes

* Merge feature 012: leva 2 — limpeza da superficie legada (1.0.0-rc.1) ([8be3810](https://www.github.com/danfma/Turfano/commit/8be3810688d2ce1d81b39287100416f390abeb7d))
* leva 2 — remove a superficie legada; core zero-dependencias; 1.0.0-rc.1 ([71d0d7c](https://www.github.com/danfma/Turfano/commit/71d0d7c335617112f7efc36df75ad78f2bc33087))
* rename DotTerritory into Turfano ([57d8de0](https://www.github.com/danfma/Turfano/commit/57d8de079d2d864e11e9e7884813e1e6bc12ed30))
* rename Territory into Turf ([13f1cc6](https://www.github.com/danfma/Turfano/commit/13f1cc6a9ff20a8a3555d9f1a1a6fd1af0879801))

