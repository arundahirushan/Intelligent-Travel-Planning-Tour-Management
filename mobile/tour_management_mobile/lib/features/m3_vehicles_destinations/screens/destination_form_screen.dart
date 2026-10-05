import 'dart:io';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../../../core/constants/sri_lanka_districts.dart';
import '../models/destination_response_dto.dart';
import '../services/destination_service.dart';

// Create or Edit destination form.
// When [existing] is null, creates a new destination (POST /api/destinations).
// When [existing] is provided, updates it (PUT /api/destinations/{id}).
//
// Fields match CreateDestinationDto / UpdateDestinationDto exactly:
//   - name (required)
//   - region (required)
//   - description (required)
//   - imageUrl (optional URL string)
//
// The photo picker is the easy way to set imageUrl: the chosen photo is uploaded
// through the API when the user presses Save, and the returned URL is sent in the
// normal JSON create/update request. The URL text field still works for now.
//
// Coordinates (latitude/longitude) are intentionally absent because neither
// CreateDestinationDto nor UpdateDestinationDto currently accepts them.
class DestinationFormScreen extends StatefulWidget {
  final DestinationResponseDto? existing;

  const DestinationFormScreen({super.key, this.existing});

  @override
  State<DestinationFormScreen> createState() => _DestinationFormScreenState();
}

class _DestinationFormScreenState extends State<DestinationFormScreen> {
  static const int _maxPhotoBytes =
      5 * 1024 * 1024; // 5 MB — the API enforces the same limit

  final _formKey = GlobalKey<FormState>();
  final DestinationService _service = DestinationService();
  final ImagePicker _picker = ImagePicker();

  late final TextEditingController _nameController;
  late final TextEditingController _descriptionController;
  late final TextEditingController _imageUrlController;

  String? _selectedDistrict;
  String? _legacyRegionMessage;

  // Newly chosen photo (not uploaded until Save is pressed).
  XFile? _pickedPhoto;
  // Remembers a successful upload so a retry after a failed save does not upload again.
  String? _uploadedPhotoPath;
  String? _uploadedPhotoUrl;

  bool _isSubmitting = false;
  bool _isUploading = false;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final d = widget.existing;
    _nameController = TextEditingController(text: d?.name ?? '');
    _descriptionController = TextEditingController(text: d?.description ?? '');
    _imageUrlController = TextEditingController(text: d?.imageUrl ?? '');

    if (d != null && d.region.isNotEmpty) {
      if (sriLankaDistricts.contains(d.region)) {
        _selectedDistrict = d.region;
      } else {
        _legacyRegionMessage =
            'Current legacy region: ${d.region}. Please select a district.';
      }
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    _imageUrlController.dispose();
    super.dispose();
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: Colors.red),
    );
  }

  // Opens the gallery. Nothing is uploaded here; upload happens when Save is pressed.
  Future<void> _pickPhoto() async {
    if (_isSubmitting) return;

    final XFile? photo;
    try {
      // Downscaling keeps typical phone photos well under the 5 MB limit.
      photo = await _picker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 1600,
        imageQuality: 85,
      );
    } catch (_) {
      if (mounted) _showError('Could not open the photo gallery.');
      return;
    }
    if (photo == null) return; // user cancelled the picker

    final lowerPath = photo.path.toLowerCase();
    final isAllowedType = lowerPath.endsWith('.jpg') ||
        lowerPath.endsWith('.jpeg') ||
        lowerPath.endsWith('.png') ||
        lowerPath.endsWith('.webp');
    if (!isAllowedType) {
      if (mounted) _showError('Only JPEG, PNG and WebP images are allowed.');
      return;
    }
    if (await photo.length() > _maxPhotoBytes) {
      if (mounted) {
        _showError('The photo is too large. The maximum size is 5 MB.');
      }
      return;
    }

    if (!mounted) return;
    setState(() => _pickedPhoto = photo);
  }

  // Preview: the newly chosen photo if there is one, otherwise the current/typed image URL.
  Widget _buildPhotoPreview() {
    final urlText = _imageUrlController.text.trim();

    Widget? image;
    if (_pickedPhoto != null) {
      image = Image.file(File(_pickedPhoto!.path), fit: BoxFit.cover);
    } else if (urlText.isNotEmpty) {
      image = Image.network(
        urlText,
        fit: BoxFit.cover,
        errorBuilder: (_, __, ___) =>
            const Center(child: Icon(Icons.broken_image, size: 48)),
      );
    }

    return Container(
      height: 180,
      decoration: BoxDecoration(
        color: Colors.grey[200],
        borderRadius: BorderRadius.circular(12),
      ),
      clipBehavior: Clip.antiAlias,
      child: image ??
          const Center(
            child: Icon(Icons.landscape_outlined, size: 48, color: Colors.grey),
          ),
    );
  }

  Future<void> _submit() async {
    if (_isSubmitting) return;
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isSubmitting = true);

    final name = _nameController.text.trim();
    final region = _selectedDistrict!;
    final description = _descriptionController.text.trim();
    // Start from the current URL so an edit without a new photo keeps the existing image.
    var imageUrl = _imageUrlController.text.trim();

    try {
      // Upload the newly chosen photo now (only on Save, never when merely picked).
      if (_pickedPhoto != null) {
        if (_uploadedPhotoPath == _pickedPhoto!.path &&
            _uploadedPhotoUrl != null) {
          // A previous Save already uploaded this photo and only the save failed.
          imageUrl = _uploadedPhotoUrl!;
        } else {
          setState(() => _isUploading = true);
          try {
            imageUrl = await _service.uploadPhoto(_pickedPhoto!.path);
          } catch (e) {
            // Stop here: do not save the destination and do not touch the existing image.
            if (mounted) {
              _showError(e.toString().replaceAll('Exception: ', ''));
            }
            return;
          } finally {
            if (mounted) setState(() => _isUploading = false);
          }
          _uploadedPhotoPath = _pickedPhoto!.path;
          _uploadedPhotoUrl = imageUrl;
        }
      }

      if (_isEditing) {
        await _service.updateDestination(
          id: widget.existing!.id,
          name: name,
          region: region,
          description: description,
          imageUrl: imageUrl.isEmpty ? null : imageUrl,
        );
      } else {
        await _service.createDestination(
          name: name,
          region: region,
          description: description,
          imageUrl: imageUrl.isEmpty ? null : imageUrl,
        );
      }

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            _isEditing
                ? 'Destination updated successfully.'
                : 'Destination created successfully.',
          ),
        ),
      );
      // Return true so the calling screen knows to refresh.
      Navigator.pop(context, true);
    } catch (e) {
      if (!mounted) return;
      _showError(e.toString().replaceAll('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_isEditing ? 'Edit Destination' : 'Create Destination'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                controller: _nameController,
                decoration: const InputDecoration(
                  labelText: 'Name *',
                  border: OutlineInputBorder(),
                ),
                textInputAction: TextInputAction.next,
                validator: (v) => (v == null || v.trim().isEmpty)
                    ? 'Name is required.'
                    : null,
              ),
              const SizedBox(height: 16),
              if (_legacyRegionMessage != null) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.orange.shade50,
                    border: Border.all(color: Colors.orange.shade200),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.warning_amber_rounded,
                          color: Colors.orange.shade700),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          _legacyRegionMessage!,
                          style: TextStyle(
                              color: Colors.orange.shade900, fontSize: 13),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
              ],
              DropdownButtonFormField<String>(
                value: _selectedDistrict,
                decoration: const InputDecoration(
                  labelText: 'District *',
                  border: OutlineInputBorder(),
                ),
                items: sriLankaDistricts
                    .map((d) => DropdownMenuItem(
                          value: d,
                          child: Text(d),
                        ))
                    .toList(),
                onChanged: (val) {
                  setState(() {
                    _selectedDistrict = val;
                    _legacyRegionMessage =
                        null; // Clear warning when they pick one
                  });
                },
                validator: (v) => v == null ? 'District is required.' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _descriptionController,
                decoration: const InputDecoration(
                  labelText: 'Description *',
                  border: OutlineInputBorder(),
                ),
                maxLines: 4,
                textInputAction: TextInputAction.next,
                validator: (v) => (v == null || v.trim().isEmpty)
                    ? 'Description is required.'
                    : null,
              ),
              const SizedBox(height: 16),
              Text('Photo', style: Theme.of(context).textTheme.titleSmall),
              const SizedBox(height: 8),
              _buildPhotoPreview(),
              const SizedBox(height: 8),
              Row(
                children: [
                  OutlinedButton.icon(
                    onPressed: _isSubmitting ? null : _pickPhoto,
                    icon: const Icon(Icons.photo_library_outlined),
                    label: Text(_pickedPhoto == null &&
                            _imageUrlController.text.trim().isEmpty
                        ? 'Choose Photo'
                        : 'Change Photo'),
                  ),
                  if (_pickedPhoto != null) ...[
                    const SizedBox(width: 8),
                    TextButton(
                      onPressed: _isSubmitting
                          ? null
                          : () => setState(() => _pickedPhoto = null),
                      child: const Text('Remove Selected Photo'),
                    ),
                  ],
                ],
              ),
              const SizedBox(height: 4),
              Text(
                'JPEG, PNG or WebP, up to 5 MB. The photo is uploaded when you press Save.',
                style: TextStyle(fontSize: 12, color: Colors.grey[600]),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _imageUrlController,
                decoration: InputDecoration(
                  labelText: 'Or paste an Image URL (optional)',
                  hintText: 'https://...',
                  helperText: _pickedPhoto != null
                      ? 'The selected photo will be used instead of this URL.'
                      : null,
                  border: const OutlineInputBorder(),
                ),
                keyboardType: TextInputType.url,
                textInputAction: TextInputAction.done,
                onChanged: (_) => setState(() {}), // refresh the preview
                onFieldSubmitted: (_) => _submit(),
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _isSubmitting ? null : _submit,
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                ),
                child: _isSubmitting
                    ? Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          ),
                          if (_isUploading) ...[
                            const SizedBox(width: 12),
                            const Text('Uploading photo...'),
                          ],
                        ],
                      )
                    : Text(
                        _isEditing ? 'Save Changes' : 'Create Destination',
                        style: const TextStyle(fontSize: 16),
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
