# Linear pattern recognition
# this is a readme
The Code is developed to support the findings of our submitted paper entitled "Linear building pattern recognition in topographical maps using convex polygon decomposition"

The tools were implemented in C# on ArcGIS 10.2 software (ESRI, USA), each tool has a separate form to set the input, output and parameters for the algorithms.

The tool for linear pattern recognition, and 4 tools are included. As follows

(1) Tool for proximity graph generation based on CDT and skeletons considering roads, including CDT, DT like proximity graph, GG, RNG, MST and NNG. 

(2) Tool for parameter computation for building characteristics, including 24 parameters of building polygons. Details for the proximity graphs and parameters of buildings' characteristics refer to:
1) Wei Z, Guo Q, Wang L, et al. On the spatial distribution of buildings for map generalization[J]. Cartography and Geographic Information Science, 2018, 45(6): 539-555.
https://www.tandfonline.com/doi/abs/10.1080/15230406.2018.1433068?casa_token=R-2k-CETpaoAAAAA:sLyJdI_1DB-Tz848gPjgwX24byKF6tyBF5mWSL-PXXw-9Z4JXgkIXexwyKdKtEufcZfQdqskoG_wbWA
2) 郭庆胜, 魏智威, 王勇, 王琳. 特征分类与邻近图相结合的建筑物群空间分布特征提取方法[J]. 测绘学报, 2017, 46(5): 631-638.（GUO Qingsheng, WEI Zhiwei, WANG Yong, WANG Lin. The Method of Extracting Spatial Distribution Characteristics of Buildings Combined with Feature Classification and Proximity Graph[J]. Acta Geodaetica et Cartographica Sinica, 2017, 46(5): 631-638.）http://xb.sinomaps.com/CN/10.11947/j.AGCS.2017.20160374

(3) Tool for convex building polygon decomposition.Details for building decomposition, refer to: 顾及结构特征的建筑物图形迭代凸分解方法-An iterative approach for building convex decomposition considering their shape characteristics，upcoming in 测绘科学

(4) Tool for linear pattern recognition, including collinear and curvilinear patterns.
