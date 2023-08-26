# Terraforming-Sebastian-Lague-Remix

This project expects to be a implementation of the marching cubes algorithm for creating 3D digital terrains.
The terrain is broken down into multiple chunks that can be generated infinitely along the ground plane and on the vertical axis.

Every chunk has it's own density texture, rather than having a single density texture for the whole world, as in Sebastian Lague's project.

However, the terraforming function is not implementated.
And there is a bug with using blurMap, as the edges of neighbouring chunks don't match up. This is perhaps because of the use of a density texture for each chunk.
